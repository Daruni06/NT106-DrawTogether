// Man hinh ve chung chinh.
// Hien canvas, thanh cong cu, chat, danh sach thanh vien va xu ly import/export anh.
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using DrawTogether.Client.Drawing;
using DrawTogether.Shared.Models;

namespace DrawTogether.Client.Forms;

public sealed class DrawingForm : Form
{
    private readonly CanvasState _canvasState = new();
    private readonly DrawingTool _drawingTool = new();
    private readonly Panel _canvasPanel = new();
    private readonly ComboBox _toolSelector = new();
    private readonly Button _colorButton = new();
    private readonly NumericUpDown _thicknessInput = new();
    private readonly Button _undoButton = new();
    private readonly Button _clearButton = new();
    private readonly Button _importButton = new();
    private readonly Button _exportButton = new();
    private readonly FlowLayoutPanel _chatMessagesPanel = new();
    private readonly TextBox _chatInput = new();
    private readonly Button _sendChatButton = new();
    private readonly Button _attachFileButton = new();
    private readonly HashSet<string> _renderedChatMessageIds = new();

    private Stroke? _currentStroke;
    private bool _isDrawing;

    public DrawingForm(string? roomId = null, string? userId = null, string? displayName = null)
    {
        RoomId = roomId;
        UserId = userId;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? userId ?? "Me" : displayName;

        Text = "Draw Together";
        Width = 1024;
        Height = 768;
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
        WireEvents();
    }

    public event EventHandler<StrokeCompletedEventArgs>? StrokeCompleted;

    public event EventHandler? ClearRequested;

    public event EventHandler<StrokeUndoEventArgs>? UndoRequested;

    public event EventHandler<ChatMessageEventArgs>? ChatMessageSubmitted;

    public string? RoomId { get; set; }

    public string? UserId { get; set; }

    public string DisplayName { get; set; }

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _canvasState.Dispose();
        }

        base.Dispose(disposing);
    }

    private void BuildLayout()
    {
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(8),
            BackColor = Color.FromArgb(245, 245, 245)
        };

        _toolSelector.DropDownStyle = ComboBoxStyle.DropDownList;
        _toolSelector.Width = 130;
        _toolSelector.DataSource = Enum.GetValues<DrawingToolType>();

        _colorButton.Text = "Color";
        _colorButton.Width = 80;
        _colorButton.BackColor = _drawingTool.Color;
        _colorButton.ForeColor = Color.White;

        _thicknessInput.Minimum = 1;
        _thicknessInput.Maximum = 50;
        _thicknessInput.Value = _drawingTool.Thickness;
        _thicknessInput.Width = 64;

        _undoButton.Text = "Undo";
        _clearButton.Text = "Clear";
        _importButton.Text = "Import";
        _exportButton.Text = "Export";

        toolbar.Controls.AddRange(new Control[]
        {
            new Label { Text = "Tool", AutoSize = true, Padding = new Padding(0, 7, 0, 0) },
            _toolSelector,
            _colorButton,
            new Label { Text = "Size", AutoSize = true, Padding = new Padding(8, 7, 0, 0) },
            _thicknessInput,
            _undoButton,
            _clearButton,
            _importButton,
            _exportButton
        });

        var mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 6,
            //Panel2MinSize = 280
        };

        mainSplit.Width = 800;
        mainSplit.Panel1MinSize = 25;
        mainSplit.Panel2MinSize = 280;
        mainSplit.SplitterDistance = 740;

        _canvasPanel.Dock = DockStyle.Fill;
        _canvasPanel.BackColor = Color.White;
        _canvasPanel.Cursor = Cursors.Cross;
        _canvasPanel.DoubleBuffered(true);

        mainSplit.Panel1.Controls.Add(_canvasPanel);
        mainSplit.Panel2.Controls.Add(BuildChatPanel());

        Controls.Add(mainSplit);
        Controls.Add(toolbar);
    }

    private void WireEvents()
    {
        _canvasState.Changed += (_, _) => _canvasPanel.Invalidate();

        _toolSelector.SelectedValueChanged += (_, _) =>
        {
            if (_toolSelector.SelectedItem is DrawingToolType toolType)
            {
                _drawingTool.SetTool(toolType);
            }
        };

        _colorButton.Click += (_, _) => ChooseColor();

        _thicknessInput.ValueChanged += (_, _) =>
        {
            _drawingTool.SetThickness((int)_thicknessInput.Value);
        };

        _undoButton.Click += (_, _) => UndoLocalStroke();
        _clearButton.Click += (_, _) => ClearCanvas();
        _importButton.Click += (_, _) => ImportCanvasImage();
        _exportButton.Click += (_, _) => ExportCanvasImage();
        _sendChatButton.Click += (_, _) => SendTextChat();
        _attachFileButton.Click += (_, _) => SendFileChat();
        _chatInput.KeyDown += (_, args) =>
        {
            if (args.KeyCode == Keys.Enter && !args.Shift)
            {
                args.SuppressKeyPress = true;
                SendTextChat();
            }
        };

        _canvasPanel.Paint += (_, args) =>
        {
            _canvasState.Render(args.Graphics, _canvasPanel.ClientSize, _currentStroke);
        };

        _canvasPanel.MouseDown += (_, args) =>
        {
            if (args.Button != MouseButtons.Left)
            {
                return;
            }

            _isDrawing = true;
            _currentStroke = _drawingTool.BeginStroke(args.Location, RoomId, UserId);
            _canvasPanel.Capture = true;
            _canvasPanel.Invalidate();
        };

        _canvasPanel.MouseMove += (_, args) =>
        {
            if (!_isDrawing || _currentStroke is null)
            {
                return;
            }

            _drawingTool.AddPoint(_currentStroke, args.Location);
            _canvasPanel.Invalidate();
        };

        _canvasPanel.MouseUp += (_, args) =>
        {
            if (!_isDrawing || _currentStroke is null)
            {
                return;
            }

            _drawingTool.CompleteStroke(_currentStroke, args.Location);
            var completedStroke = _currentStroke.Clone();

            _canvasState.AddStroke(completedStroke);
            StrokeCompleted?.Invoke(this, new StrokeCompletedEventArgs(completedStroke));

            _currentStroke = null;
            _isDrawing = false;
            _canvasPanel.Capture = false;
            _canvasPanel.Invalidate();
        };
    }

    private void ChooseColor()
    {
        using var dialog = new ColorDialog
        {
            Color = _drawingTool.Color,
            FullOpen = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _drawingTool.SetColor(dialog.Color);
        _colorButton.BackColor = dialog.Color;
        _colorButton.ForeColor = dialog.Color.GetBrightness() < 0.5 ? Color.White : Color.Black;
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

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

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

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _canvasState.SavePng(dialog.FileName, _canvasPanel.ClientSize);
    }

    private Control BuildChatPanel()
    {
        var chatPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = Color.WhiteSmoke,
            Padding = new Padding(8)
        };

        chatPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        chatPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        chatPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));

        var title = new Label
        {
            Text = "Room Chat",
            Dock = DockStyle.Fill,
            Font = new Font(Font, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _chatMessagesPanel.Dock = DockStyle.Fill;
        _chatMessagesPanel.AutoScroll = true;
        _chatMessagesPanel.FlowDirection = FlowDirection.TopDown;
        _chatMessagesPanel.WrapContents = false;
        _chatMessagesPanel.BackColor = Color.White;
        _chatMessagesPanel.Padding = new Padding(8);

        var inputPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 3,
            Padding = new Padding(0, 8, 0, 0)
        };

        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));

        _attachFileButton.Text = "+";
        _attachFileButton.Dock = DockStyle.Fill;

        _chatInput.Multiline = true;
        _chatInput.Dock = DockStyle.Fill;
        _chatInput.PlaceholderText = "Nhap tin nhan...";

        _sendChatButton.Text = "Send";
        _sendChatButton.Dock = DockStyle.Fill;

        inputPanel.Controls.Add(_attachFileButton, 0, 0);
        inputPanel.Controls.Add(_chatInput, 1, 0);
        inputPanel.Controls.Add(_sendChatButton, 2, 0);

        chatPanel.Controls.Add(title, 0, 0);
        chatPanel.Controls.Add(_chatMessagesPanel, 0, 1);
        chatPanel.Controls.Add(inputPanel, 0, 2);

        return chatPanel;
    }

    private void SendTextChat()
    {
        var content = _chatInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var message = ChatMessage.CreateText(RoomId, UserId, DisplayName, content);
        _chatInput.Clear();
        AddChatBubble(message);
        ChatMessageSubmitted?.Invoke(this, new ChatMessageEventArgs(message));
    }

    private void SendFileChat()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Send file",
            Filter = "All files|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var fileInfo = new FileInfo(dialog.FileName);

        if (fileInfo.Length > 5 * 1024 * 1024)
        {
            MessageBox.Show(this, "File toi da 5 MB de demo gui qua JSON base64.", "File too large");
            return;
        }

        var bytes = File.ReadAllBytes(dialog.FileName);
        var attachment = ChatAttachment.FromBytes(
            fileInfo.Name,
            GuessContentType(fileInfo.Extension),
            bytes);

        var caption = _chatInput.Text.Trim();
        var message = ChatMessage.CreateFile(RoomId, UserId, DisplayName, attachment, caption);

        _chatInput.Clear();
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

        if (!_renderedChatMessageIds.Add(message.MessageId))
        {
            return;
        }

        var isMine = !string.IsNullOrWhiteSpace(UserId) && message.SenderId == UserId;
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            Width = Math.Max(220, _chatMessagesPanel.ClientSize.Width - 28),
            FlowDirection = isMine ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 4, 0, 4)
        };

        var bubble = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(10),
            MaximumSize = new Size(260, 0),
            BackColor = isMine ? Color.FromArgb(0, 132, 255) : Color.FromArgb(235, 235, 235)
        };

        var foreColor = isMine ? Color.White : Color.Black;

        if (!isMine)
        {
            bubble.Controls.Add(new Label
            {
                Text = message.SenderName,
                AutoSize = true,
                ForeColor = Color.DimGray,
                Font = new Font(Font, FontStyle.Bold)
            });
        }

        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            bubble.Controls.Add(new Label
            {
                Text = message.Content,
                AutoSize = true,
                MaximumSize = new Size(230, 0),
                ForeColor = foreColor
            });
        }

        if (message.Attachment is not null)
        {
            AddAttachmentControls(bubble, message.Attachment, foreColor);
        }

        bubble.Controls.Add(new Label
        {
            Text = message.SentAt.ToLocalTime().ToString("HH:mm"),
            AutoSize = true,
            ForeColor = isMine ? Color.FromArgb(220, 235, 255) : Color.Gray,
            Font = new Font(Font.FontFamily, 7)
        });

        row.Controls.Add(bubble);
        _chatMessagesPanel.Controls.Add(row);
        _chatMessagesPanel.ScrollControlIntoView(row);
    }

    private void AddAttachmentControls(FlowLayoutPanel bubble, ChatAttachment attachment, Color foreColor)
    {
        if (attachment.IsImage)
        {
            try
            {
                using var stream = new MemoryStream(attachment.GetBytes());
                using var image = Image.FromStream(stream);

                bubble.Controls.Add(new PictureBox
                {
                    Image = new Bitmap(image),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Width = 220,
                    Height = 130,
                    BackColor = Color.Black
                });
            }
            catch
            {
                // Image preview is optional. Download link still works.
            }
        }

        var downloadButton = new Button
        {
            Text = $"Download {attachment.FileName} ({FormatBytes(attachment.Size)})",
            AutoSize = true,
            MaximumSize = new Size(230, 0),
            ForeColor = foreColor
        };

        downloadButton.Click += (_, _) => DownloadAttachment(attachment);
        bubble.Controls.Add(downloadButton);
    }

    private void DownloadAttachment(ChatAttachment attachment)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Save chat file",
            FileName = attachment.FileName,
            Filter = "All files|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        File.WriteAllBytes(dialog.FileName, attachment.GetBytes());
    }

    private static string GuessContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".txt" => "text/plain",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024d:0.0} KB";
        }

        return $"{bytes / 1024d / 1024d:0.0} MB";
    }
}

public sealed class StrokeCompletedEventArgs : EventArgs
{
    public StrokeCompletedEventArgs(Stroke stroke)
    {
        Stroke = stroke;
    }

    public Stroke Stroke { get; }
}

public sealed class StrokeUndoEventArgs : EventArgs
{
    public StrokeUndoEventArgs(string strokeId)
    {
        StrokeId = strokeId;
    }

    public string StrokeId { get; }
}

public sealed class ChatMessageEventArgs : EventArgs
{
    public ChatMessageEventArgs(ChatMessage message)
    {
        Message = message;
    }

    public ChatMessage Message { get; }
}

internal static class ControlExtensions
{
    public static void DoubleBuffered(this Control control, bool enabled)
    {
        var property = typeof(Control).GetProperty(
            "DoubleBuffered",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        property?.SetValue(control, enabled, null);
    }
}