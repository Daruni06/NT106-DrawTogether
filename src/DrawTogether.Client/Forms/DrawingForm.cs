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

            // Add canvas and ensure it's behind the toolbar (toolbar must remain interactive)
            Controls.Add(_canvasPanel);
            _canvasPanel.SendToBack();

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
            // Ensure chat flow stacks messages vertically and scrolls
            try
            {
                flowChatMsg.FlowDirection = FlowDirection.TopDown;
                flowChatMsg.WrapContents = false;
                flowChatMsg.AutoScroll = true;
            }
            catch
            {
                // ignore if control missing or designer set differently
            }

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
            btnRedo.Click += (_, _) => RedoLocalStroke();
            btnClear.Click += (_, _) => { ClearCanvas(); };
            btnImport.Click += (_, _) => ImportCanvasImage();
            btnExport.Click += (_, _) => ExportCanvasImage();

            // Tools (tile buttons) - set drawing tool when clicked
            btnPen.Click += (_, _) => _drawingTool.SetTool(DrawingToolType.Pen);
            //btnBrush.Click += (_, _) => _drawingTool.SetTool(DrawingToolType.Pen);
            btnEraser.Click += (_, _) => _drawingTool.SetTool(DrawingToolType.Eraser);
            //btnFill.Click += (_, _) => _drawingTool.SetTool(DrawingToolType.Rectangle); // keep mapping, adjust as needed
            btnLine.Click += (_, _) => _drawingTool.SetTool(DrawingToolType.Line);
            //btnCurve.Click += (_, _) => _drawingTool.SetTool(DrawingToolType.Pen);
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

        private void RedoLocalStroke()
        {
            var restored = _canvasState.RedoLast(UserId);

            if (restored is not null)
            {
                // Notify listeners as if a stroke was completed locally
                StrokeCompleted?.Invoke(this, new StrokeCompletedEventArgs(restored.Clone()));
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
            // Container panel for message
            var bubble = new Panel { AutoSize = true, BackColor = isMine ? Color.FromArgb(46, 204, 113) : Color.FromArgb(230, 230, 230), Padding = new Padding(8), Margin = new Padding(6) };
            var availableWidth = Math.Max(200, flowChatMsg.ClientSize.Width - 24);
            bubble.MaximumSize = new Size(availableWidth, 0);

            // Use inner vertical flow panel to avoid absolute positioning collisions
            var contentFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                MaximumSize = new Size(bubble.MaximumSize.Width - 16, 0),
                Margin = new Padding(0)
            };

            // Header with sender name and time
            var header = new Label
            {
                Text = $"{message.SenderName} • {message.SentAt.ToLocalTime():HH:mm}",
                AutoSize = true,
                Font = new Font(Font.FontFamily, 9f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 4)
            };
            header.ForeColor = isMine ? Color.White : Color.Black;
            contentFlow.Controls.Add(header);

            // Content text
            if (!string.IsNullOrWhiteSpace(message.Content))
            {
                var contentLabel = new Label
                {
                    Text = message.Content.Trim(),
                    AutoSize = true,
                    MaximumSize = new Size(contentFlow.MaximumSize.Width, 0),
                    Font = new Font(Font.FontFamily, 10f, FontStyle.Regular),
                    Margin = new Padding(0, 2, 0, 4)
                };
                contentLabel.ForeColor = isMine ? Color.White : Color.Black;
                contentFlow.Controls.Add(contentLabel);
            }

            // Attachment handling
            if (message.Attachment is not null)
            {
                try
                {
                    var bytes = message.Attachment.GetBytes();
                    if (message.Attachment.IsImage)
                    {
                        using var ms = new MemoryStream(bytes);
                        var img = Image.FromStream(ms);
                        var thumb = new PictureBox
                        {
                            SizeMode = PictureBoxSizeMode.Zoom,
                            Width = Math.Min(availableWidth - 16, img.Width),
                            Height = Math.Min(180, img.Height),
                            Margin = new Padding(0, 6, 0, 6),
                            BorderStyle = BorderStyle.FixedSingle
                        };
                        thumb.Image = new Bitmap(img);
                        contentFlow.Controls.Add(thumb);

                        // Download button
                        var btn = new Button
                        {
                            Text = "Download",
                            AutoSize = true,
                            Margin = new Padding(0, 4, 0, 0),
                            BackColor = Color.FromArgb(40, 116, 240),
                            ForeColor = Color.White,
                            FlatStyle = FlatStyle.Standard
                        };
                        btn.Click += (_, _) => DownloadAttachment(message.Attachment);
                        contentFlow.Controls.Add(btn);
                    }
                    else
                    {
                        var attPanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, 6, 0, 0) };
                        var attLabel = new Label { Text = $"{message.Attachment.FileName} ({FormatBytes(message.Attachment.Size)})", AutoSize = true };
                        var btn = new Button { Text = "Download", AutoSize = true, Margin = new Padding(8, 0, 0, 0), BackColor = Color.FromArgb(40, 116, 240), ForeColor = Color.White, FlatStyle = FlatStyle.Standard };
                        btn.Click += (_, _) => DownloadAttachment(message.Attachment);
                        attPanel.Controls.Add(attLabel);
                        attPanel.Controls.Add(btn);
                        contentFlow.Controls.Add(attPanel);
                    }
                }
                catch
                {
                    var errLabel = new Label { Text = $"Attachment: {message.Attachment.FileName}", AutoSize = true };
                    contentFlow.Controls.Add(errLabel);
                }
            }

            bubble.Controls.Add(contentFlow);

            // Adjust colors for header/content when own message is highlighted
            if (isMine)
            {
                bubble.BorderStyle = BorderStyle.None;
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
