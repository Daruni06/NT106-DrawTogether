using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DrawTogether.Client.Drawing;
using DrawTogether.Shared.Models;

namespace DrawTogether.Client.Forms
{
    public partial class DrawingForm : Form
    {
        private readonly CanvasState _canvasState = new();
        private readonly DrawingTool _drawingTool = new();
        private readonly Panel _canvasPanel = new();
        private readonly HashSet<string> _renderedChatMessageIds = new();
        private bool _isDrawing;
        private Stroke? _currentStroke;

        public DrawingForm(string? roomId = null, string? userId = null, string? displayName = null)
        {
            RoomId = roomId;
            UserId = userId;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? userId ?? "Me" : displayName;

            InitializeComponent();

            BuildCanvasAndLayout();
            WireEventsToDesignerControls();
        }

        private const int ChatPanelWidth = 400;

        private void BuildCanvasAndLayout()
        {
            // Canvas panel
            _canvasPanel.Dock = DockStyle.Fill;
            _canvasPanel.BackColor = Color.White;
            _canvasPanel.Cursor = Cursors.Cross;
            _canvasPanel.DoubleBuffered(true);

            // Add canvas under top toolbar (designer-created `guna2Panel1` should be above index 0)
            Controls.Add(_canvasPanel);
            Controls.SetChildIndex(_canvasPanel, 0);

            // Configure chat panel as an overlay anchored to the right. Do NOT dock it
            // so it doesn't reserve layout space when hidden.
            try
            {
                panelChat.Visible = false;
                panelChat.Width = ChatPanelWidth;
                panelChat.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
                panelChat.BorderStyle = BorderStyle.None;
                // Position it; actual X will be recalculated on resize/Shown
                panelChat.Location = new Point(Math.Max(0, ClientSize.Width - panelChat.Width), guna2Panel1?.Height ?? 0);
                Controls.Add(panelChat);
                panelChat.BringToFront();
            }
            catch
            {
                // ignore if designer didn't include panelChat
            }

            // Reposition chat overlay when form size changes
            SizeChanged += (_, _) => RepositionChatPanel();
            Shown += (_, _) => RepositionChatPanel();
        }

        private void RepositionChatPanel()
        {
            if (panelChat is null) return;

            var top = guna2Panel1?.Height ?? 0;
            var x = Math.Max(0, ClientSize.Width - panelChat.Width - 8);
            panelChat.Location = new Point(x, top);
            panelChat.Height = Math.Max(100, ClientSize.Height - top - 8);
            if (panelChat.Visible) panelChat.BringToFront();
        }

        public event EventHandler<StrokeCompletedEventArgs>? StrokeCompleted;
        public event EventHandler? ClearRequested;
        public event EventHandler<StrokeUndoEventArgs>? UndoRequested;
        public event EventHandler<ChatMessageEventArgs>? ChatMessageSubmitted;

        public string? RoomId { get; set; }
        public string? UserId { get; set; }
        public string DisplayName { get; set; }

        #region Remote apply helpers
        public void ApplyRemoteStroke(Stroke stroke)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => ApplyRemoteStroke(stroke)));
                return;
            }

            _canvasState.AddStroke(stroke);
        }

        public void ApplyRemoteClear()
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)ApplyRemoteClear);
                return;
            }

            _canvasState.Clear();
        }

        public void ApplyRemoteUndo(string strokeId)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => ApplyRemoteUndo(strokeId)));
                return;
            }

            var remaining = _canvasState.Strokes
                .Where(stroke => stroke.StrokeId != strokeId)
                .Select(stroke => stroke.Clone())
                .ToList();

            _canvasState.SetHistory(remaining);
        }

        public void LoadHistory(IEnumerable<Stroke> strokes)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => LoadHistory(strokes)));
                return;
            }

            _canvasState.SetHistory(strokes);
        }

        public void ApplyRemoteChatMessage(ChatMessage message)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => ApplyRemoteChatMessage(message)));
                return;
            }

            AddChatBubble(message);
        }
        #endregion

        private void WireEventsToDesignerControls()
        {
            // Canvas rendering and mouse
            _canvasState.Changed += (_, _) => _canvasPanel.Invalidate();

            _canvasPanel.Paint += (_, args) => _canvasState.Render(args.Graphics, _canvasPanel.ClientSize, _currentStroke);

            _canvasPanel.MouseDown += (_, args) =>
            {
                if (args.Button != MouseButtons.Left) return;

                _isDrawing = true;
                _currentStroke = _drawingTool.BeginStroke(args.Location, RoomId, UserId);
                _canvasPanel.Capture = true;
                _canvasPanel.Invalidate();
            };

            _canvasPanel.MouseMove += (_, args) =>
            {
                if (!_isDrawing || _currentStroke is null) return;

                _drawingTool.AddPoint(_currentStroke, args.Location);
                _canvasPanel.Invalidate();
            };

            _canvasPanel.MouseUp += (_, args) =>
            {
                if (!_isDrawing || _currentStroke is null) return;

                _drawingTool.CompleteStroke(_currentStroke, args.Location);
                var completedStroke = _currentStroke.Clone();

                _canvasState.AddStroke(completedStroke);
                StrokeCompleted?.Invoke(this, new StrokeCompletedEventArgs(completedStroke));

                _currentStroke = null;
                _isDrawing = false;
                _canvasPanel.Capture = false;
                _canvasPanel.Invalidate();
            };

            // Toolbar actions
            btnUndo.Click += (_, _) => UndoLocalStroke();
            btnRedo.Click += (_, _) => { /* redo not implemented yet */ };
            btnClear.Click += (_, _) => { ClearCanvas(); };
            btnImport.Click += (_, _) => ImportCanvasImage();
            btnExport.Click += (_, _) => ExportCanvasImage();

            // Tools (tile buttons) - set drawing tool when clicked
            btnPen.Click += (_, _) => _drawingTool.SetTool(DrawingToolType.Pen);
            btnBrush.Click += (_, _) => _drawingTool.SetTool(DrawingToolType.Pen);
            btnEraser.Click += (_, _) => _drawingTool.SetTool(DrawingToolType.Eraser);
            btnFill.Click += (_, _) => _drawingTool.SetTool(DrawingToolType.Rectangle); // keep mapping, adjust as needed
            btnLine.Click += (_, _) => _drawingTool.SetTool(DrawingToolType.Line);
            btnCurve.Click += (_, _) => _drawingTool.SetTool(DrawingToolType.Pen);
            btnEllipse.Click += (_, _) => _drawingTool.SetTool(DrawingToolType.Ellipse);
            btnRectangle.Click += (_, _) => _drawingTool.SetTool(DrawingToolType.Rectangle);

            // Size controls
            btnTrackSize.Minimum = 1;
            btnTrackSize.Maximum = 50;
            btnTrackSize.Value = _drawingTool.Thickness;
            btnNumSize.Value = _drawingTool.Thickness;

            btnTrackSize.Scroll += (_, _) => btnNumSize.Value = btnTrackSize.Value;
            btnNumSize.ValueChanged += (_, _) => btnTrackSize.Value = (int)btnNumSize.Value;
            btnNumSize.ValueChanged += (_, _) => _drawingTool.SetThickness((int)btnNumSize.Value);

            // Color selection: open ColorDialog and set btnColor fill
            btnColor.Click += (_, _) =>
            {
                using var dialog = new ColorDialog { Color = _drawingTool.Color, FullOpen = true };
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                _drawingTool.SetColor(dialog.Color);
                try
                {
                    // Guna2TileButton has FillColor property
                    btnColor.FillColor = dialog.Color;
                }
                catch
                {
                    // fallback: change BackColor if FillColor not available
                    btnColor.BackColor = dialog.Color;
                }
            };

            // Chat wiring
            btnSend.Click += (_, _) => SendTextChat();
            btnUpload.Click += (_, _) => SendFileChat();
            txtMessage.KeyDown += (_, args) =>
            {
                if (args.KeyCode == Keys.Enter && !args.Shift)
                {
                    args.SuppressKeyPress = true;
                    SendTextChat();
                }
            };

            btnChat.Click += (_, _) =>
            {
                var show = !(panelChat?.Visible ?? false);
                if (panelChat is not null)
                {
                    panelChat.Visible = show;
                    if (show)
                    {
                        RepositionChatPanel();
                        panelChat.BringToFront();
                        txtMessage.Focus();
                    }
                }
            };

            // Render initial color on btnColor
            try
            {
                btnColor.FillColor = _drawingTool.Color;
            }
            catch
            {
                btnColor.BackColor = _drawingTool.Color;
            }
        }

        private void UndoLocalStroke()
        {
            var removed = _canvasState.UndoLast(UserId);

            if (removed is not null)
            {
                UndoRequested?.Invoke(this, new StrokeUndoEventArgs(removed.StrokeId));
            }
        }

        private void ClearCanvas()
        {
            _canvasState.Clear();
            ClearRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ImportCanvasImage()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Import canvas image",
                Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp|All files|*.*"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            using var image = Image.FromFile(dialog.FileName);
            _canvasState.SetBackgroundImage(image);
        }

        private void ExportCanvasImage()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Export canvas image",
                Filter = "PNG image|*.png",
                DefaultExt = "png",
                AddExtension = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            _canvasState.SavePng(dialog.FileName, _canvasPanel.ClientSize);
        }

        private void SendTextChat()
        {
            var content = txtMessage.Text.Trim();
            if (string.IsNullOrWhiteSpace(content)) return;

            var message = ChatMessage.CreateText(RoomId, UserId, DisplayName, content);
            txtMessage.Clear();
            AddChatBubble(message);
            ChatMessageSubmitted?.Invoke(this, new ChatMessageEventArgs(message));
        }

        private void SendFileChat()
        {
            using var dialog = new OpenFileDialog { Title = "Send file", Filter = "All files|*.*" };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            var fileInfo = new FileInfo(dialog.FileName);
            if (fileInfo.Length > 5 * 1024 * 1024)
            {
                MessageBox.Show(this, "File toi da 5 MB de demo gui qua JSON base64.", "File too large");
                return;
            }

            var bytes = File.ReadAllBytes(dialog.FileName);
            var attachment = ChatAttachment.FromBytes(fileInfo.Name, GuessContentType(fileInfo.Extension), bytes);
            var caption = txtMessage.Text.Trim();
            var message = ChatMessage.CreateFile(RoomId, UserId, DisplayName, attachment, caption);

            txtMessage.Clear();
            AddChatBubble(message);
            ChatMessageSubmitted?.Invoke(this, new ChatMessageEventArgs(message));
        }

        private void AddChatBubble(ChatMessage message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AddChatBubble(message)));
                return;
            }

            if (!_renderedChatMessageIds.Add(message.MessageId)) return;

            var isMine = !string.IsNullOrWhiteSpace(UserId) && message.SenderId == UserId;

            var bubble = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            var label = new Label { Text = $"{message.SenderName}: {message.Content}", AutoSize = true, MaximumSize = new Size(flowChatMsg.Width - 24, 0) };
            bubble.Controls.Add(label);

            if (message.Attachment is not null)
            {
                var attLabel = new LinkLabel { Text = $"Attachment: {message.Attachment.FileName} ({FormatBytes(message.Attachment.Size)})", AutoSize = true };
                attLabel.Tag = message.Attachment;
                attLabel.LinkClicked += (_, _) => DownloadAttachment(message.Attachment);
                bubble.Controls.Add(attLabel);
            }

            flowChatMsg.Controls.Add(bubble);
            flowChatMsg.ScrollControlIntoView(bubble);
        }

        private void DownloadAttachment(ChatAttachment attachment)
        {
            using var dialog = new SaveFileDialog { FileName = attachment.FileName };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            File.WriteAllBytes(dialog.FileName, attachment.GetBytes());
        }

        private static string GuessContentType(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return "application/octet-stream";
            extension = extension.TrimStart('.').ToLowerInvariant();
            return extension switch
            {
                "png" => "image/png",
                "jpg" or "jpeg" => "image/jpeg",
                "gif" => "image/gif",
                "txt" => "text/plain",
                "pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024} KB";
            return $"{bytes / (1024 * 1024)} MB";
        }

        private void DrawingForm_Load(object sender, EventArgs e)
        {

        }
    }
}
