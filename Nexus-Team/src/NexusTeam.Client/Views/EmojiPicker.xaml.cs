namespace NexusTeam.Client.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Emoji picker control with categories, search, and recently used emojis.
    /// </summary>
    public partial class EmojiPicker : UserControl, INotifyPropertyChanged
    {
        private const int MaxRecentEmojis = 30;
        private readonly Dictionary<string, List<string>> emojiCategories;
        private readonly List<string> recentEmojis;
        private string searchText = string.Empty;
        private string selectedCategory = "Smileys";

        /// <summary>
        /// Initializes a new instance of the <see cref="EmojiPicker"/> class.
        /// </summary>
        public EmojiPicker()
        {
            this.InitializeComponent();
            this.DataContext = this;
            this.emojiCategories = this.InitializeEmojiCategories();
            this.recentEmojis = this.LoadRecentEmojis();
            this.Loaded += this.EmojiPicker_Loaded;
        }

        private void EmojiPicker_Loaded(object sender, RoutedEventArgs e)
        {
            // Set ItemsSource directly to ensure it works
            if (this.EmojiItemsControl != null)
            {
                this.EmojiItemsControl.ItemsSource = this.EmojiList;
            }

            this.SelectCategory("Smileys");

            // Setup horizontal scrollbar
            this.SetupHorizontalScrollbar();
        }

        private void SetupHorizontalScrollbar()
        {
            if (this.CategoryScrollViewer != null && this.CategoryScrollThumb != null)
            {
                this.CategoryScrollViewer.ScrollChanged += this.CategoryScrollViewer_ScrollChanged;
                this.CategoryScrollViewer.SizeChanged += this.CategoryScrollViewer_SizeChanged;

                // Make thumb draggable
                this.CategoryScrollThumb.MouseLeftButtonDown += this.CategoryScrollThumb_MouseLeftButtonDown;
                this.CategoryScrollThumb.MouseMove += this.CategoryScrollThumb_MouseMove;
                this.CategoryScrollThumb.MouseLeftButtonUp += this.CategoryScrollThumb_MouseLeftButtonUp;

                this.UpdateScrollThumb();
            }
        }

        private bool isDraggingThumb = false;
        private double thumbDragStartX = 0;
        private double scrollStartOffset = 0;

        private void CategoryScrollThumb_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.CategoryScrollViewer == null || this.CategoryScrollThumb == null)
            {
                return;
            }

            this.isDraggingThumb = true;
            this.thumbDragStartX = e.GetPosition(this.CategoryScrollThumb.Parent as UIElement).X;
            this.scrollStartOffset = this.CategoryScrollViewer.HorizontalOffset;
            this.CategoryScrollThumb.CaptureMouse();
            e.Handled = true;
        }

        private void CategoryScrollThumb_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!this.isDraggingThumb || this.CategoryScrollViewer == null || this.CategoryScrollThumb == null)
            {
                return;
            }

            var parent = this.CategoryScrollThumb.Parent as UIElement;
            if (parent == null)
            {
                return;
            }

            var currentX = e.GetPosition(parent).X;
            var deltaX = currentX - this.thumbDragStartX;

            var trackWidth = (parent as FrameworkElement)?.ActualWidth ?? 0;
            var scrollableWidth = this.CategoryScrollViewer.ScrollableWidth;

            if (trackWidth > 0 && scrollableWidth > 0)
            {
                var scrollDelta = (deltaX / trackWidth) * this.CategoryScrollViewer.ExtentWidth;
                var newOffset = this.scrollStartOffset + scrollDelta;
                this.CategoryScrollViewer.ScrollToHorizontalOffset(Math.Max(0, Math.Min(newOffset, scrollableWidth)));
            }
        }

        private void CategoryScrollThumb_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.CategoryScrollThumb != null)
            {
                this.CategoryScrollThumb.ReleaseMouseCapture();
            }

            this.isDraggingThumb = false;
        }

        private void CategoryScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            this.UpdateScrollThumb();
        }

        private void CategoryScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.UpdateScrollThumb();
        }

        private void CategoryScrollTrack_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.CategoryScrollViewer == null || this.CategoryScrollThumb == null || this.CategoryScrollTrack == null)
            {
                return;
            }

            // Don't scroll if clicking on thumb
            var thumbRect = new Rect(
                this.CategoryScrollThumb.TranslatePoint(new Point(0, 0), this.CategoryScrollTrack),
                new Size(this.CategoryScrollThumb.ActualWidth, this.CategoryScrollThumb.ActualHeight));

            var clickPoint = e.GetPosition(this.CategoryScrollTrack);
            if (thumbRect.Contains(clickPoint))
            {
                return;
            }

            // Scroll to clicked position
            var trackWidth = this.CategoryScrollTrack.ActualWidth;
            var scrollableWidth = this.CategoryScrollViewer.ScrollableWidth;

            if (trackWidth > 0 && scrollableWidth > 0)
            {
                var clickRatio = clickPoint.X / trackWidth;
                var newOffset = clickRatio * scrollableWidth;
                this.CategoryScrollViewer.ScrollToHorizontalOffset(Math.Max(0, Math.Min(newOffset, scrollableWidth)));
            }
        }

        private void UpdateScrollThumb()
        {
            if (this.CategoryScrollViewer == null || this.CategoryScrollThumb == null)
            {
                return;
            }

            var scrollableWidth = this.CategoryScrollViewer.ScrollableWidth;
            var viewportWidth = this.CategoryScrollViewer.ViewportWidth;
            var extentWidth = this.CategoryScrollViewer.ExtentWidth;
            var horizontalOffset = this.CategoryScrollViewer.HorizontalOffset;

            var track = this.CategoryScrollThumb.Parent as FrameworkElement;
            if (track == null || track.ActualWidth <= 0 || extentWidth <= 0)
            {
                this.CategoryScrollThumb.Visibility = Visibility.Collapsed;
                return;
            }

            this.CategoryScrollThumb.Visibility = scrollableWidth > 0 ? Visibility.Visible : Visibility.Collapsed;

            if (scrollableWidth > 0)
            {
                // Calculate thumb width based on viewport/extent ratio
                var thumbWidth = Math.Max(20, (viewportWidth / extentWidth) * track.ActualWidth);
                this.CategoryScrollThumb.Width = thumbWidth;

                // Calculate thumb position based on scroll offset
                var maxThumbPosition = track.ActualWidth - thumbWidth;
                var thumbPosition = (horizontalOffset / scrollableWidth) * maxThumbPosition;
                this.CategoryScrollThumb.HorizontalAlignment = HorizontalAlignment.Left;
                this.CategoryScrollThumb.Margin = new Thickness(thumbPosition, 0, 0, 0);
            }
        }

        /// <summary>
        /// Event raised when an emoji is selected.
        /// </summary>
        public event EventHandler<string>? EmojiSelected;

        /// <summary>
        /// Gets or sets the search text.
        /// </summary>
        public string SearchText
        {
            get => this.searchText;
            set
            {
                if (this.SetProperty(ref this.searchText, value))
                {
                    this.UpdateEmojiList();
                }
            }
        }

        /// <summary>
        /// Gets the current emoji list to display.
        /// </summary>
        public ObservableCollection<string> EmojiList { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Initializes emoji categories with emojis.
        /// </summary>
        private Dictionary<string, List<string>> InitializeEmojiCategories()
        {
            return new Dictionary<string, List<string>>
            {
                ["Smileys"] = new List<string>
                {
                    "😀", "😃", "😄", "😁", "😆", "😅", "🤣", "😂", "🙂", "🙃", "😉", "😊", "😇", "🥰", "😍", "🤩",
                    "😘", "😗", "😚", "😙", "😋", "😛", "😜", "🤪", "😝", "🤑", "🤗", "🤭", "🤫", "🤔", "🤐", "🤨",
                    "😐", "😑", "😶", "😏", "😒", "🙄", "😬", "🤥", "😌", "😔", "😪", "🤤", "😴", "😷", "🤒", "🤕",
                    "🤢", "🤮", "🤧", "🥵", "🥶", "😶‍🌫️", "😵", "😵‍💫", "🤯", "🤠", "🥳", "😎", "🤓", "🧐", "😕", "😟",
                    "🙁", "☹️", "😮", "😯", "😲", "😳", "🥺", "😦", "😧", "😨", "😰", "😥", "😢", "😭", "😱", "😖",
                    "😣", "😞", "😓", "😩", "😫", "🥱", "😤", "😡", "😠", "🤬", "😈", "👿", "💀", "☠️", "💩", "🤡",
                    "👹", "👺", "👻", "👽", "👾", "🤖", "😺", "😸", "😹", "😻", "😼", "😽", "🙀", "😿", "😾",
                },
                ["People"] = new List<string>
                {
                    "👶", "🧒", "👦", "👧", "🧑", "👱", "👨", "🧔", "👱‍♂️", "👨‍🦰", "👨‍🦱", "👨‍🦳", "👨‍🦲", "👩", "👱‍♀️", "👩‍🦰",
                    "🧑‍🦰", "👩‍🦱", "🧑‍🦱", "👩‍🦳", "🧑‍🦳", "👩‍🦲", "🧑‍🦲", "👱", "🧓", "👴", "👵", "🙍", "🙍‍♂️", "🙍‍♀️", "🙎",
                    "🙎‍♂️", "🙎‍♀️", "🙅", "🙅‍♂️", "🙅‍♀️", "🙆", "🙆‍♂️", "🙆‍♀️", "💁", "💁‍♂️", "💁‍♀️", "🙋", "🙋‍♂️", "🙋‍♀️", "🧏", "🧏‍♂️",
                    "🧏‍♀️", "🤦", "🤦‍♂️", "🤦‍♀️", "🤷", "🤷‍♂️", "🤷‍♀️", "🙇", "🙇‍♂️", "🙇‍♀️", "🤦", "🤦‍♂️", "🤦‍♀️", "🤷", "🤷‍♂️", "🤷‍♀️",
                },
                ["Animals"] = new List<string>
                {
                    "🐶", "🐱", "🐭", "🐹", "🐰", "🦊", "🐻", "🐼", "🐨", "🐯", "🦁", "🐮", "🐷", "🐽", "🐸", "🐵",
                    "🙈", "🙉", "🙊", "🐒", "🐔", "🐧", "🐦", "🐤", "🐣", "🐥", "🦆", "🦅", "🦉", "🦇", "🐺", "🐗",
                    "🐴", "🦄", "🐝", "🐛", "🦋", "🐌", "🐞", "🐜", "🦟", "🦗", "🕷️", "🦂", "🐢", "🐍", "🦎", "🦖",
                    "🦕", "🐙", "🦑", "🦐", "🦞", "🦀", "🐡", "🐠", "🐟", "🐬", "🐳", "🐋", "🦈", "🐊", "🐅", "🐆",
                    "🦓", "🦍", "🦧", "🐘", "🦛", "🦏", "🐪", "🐫", "🦒", "🦘", "🦡", "🐃", "🐂", "🐄", "🐎", "🐖",
                    "🐏", "🐑", "🦙", "🐐", "🦌", "🐕", "🐩", "🦮", "🐕‍🦺", "🐈", "🐓", "🦃", "🦅", "🦆", "🦢", "🦉",
                    "🦚", "🦜", "🦇", "🦝", "🦨", "🦦", "🦥", "🐿️", "🦔",
                },
                ["Food"] = new List<string>
                {
                    "🍏", "🍎", "🍐", "🍊", "🍋", "🍌", "🍉", "🍇", "🍓", "🍈", "🍒", "🍑", "🥭", "🍍", "🥥", "🥝",
                    "🍅", "🍆", "🥑", "🥦", "🥬", "🥒", "🌶️", "🌽", "🥕", "🥔", "🍠", "🥐", "🥯", "🍞", "🥖", "🥨",
                    "🧀", "🥚", "🍳", "🥞", "🥓", "🥩", "🍗", "🍖", "🦴", "🌭", "🍔", "🍟", "🍕", "🥪", "🥙", "🌮",
                    "🌯", "🥗", "🥘", "🥫", "🍝", "🍜", "🍲", "🍛", "🍣", "🍱", "🥟", "🥠", "🥡", "🍤", "🍙", "🍚",
                    "🍘", "🍥", "🥮", "🍢", "🍡", "🍧", "🍨", "🍦", "🥧", "🍰", "🎂", "🍮", "🍭", "🍬", "🍫", "🍿",
                    "🍩", "🍪", "🌰", "🥜", "🍯", "🥛", "🍼", "☕", "🍵", "🧃", "🥤", "🍶", "🍺", "🍻", "🥂", "🍷",
                    "🥃", "🍸", "🍹", "🧉", "🍾", "🧊",
                },
                ["Travel"] = new List<string>
                {
                    "🚗", "🚕", "🚙", "🚌", "🚎", "🏎️", "🚓", "🚑", "🚒", "🚐", "🚚", "🚛", "🚜", "🛴", "🚲", "🛵",
                    "🏍️", "🛺", "🚨", "🚔", "🚍", "🚘", "🚖", "🚡", "🚠", "🚟", "🚃", "🚋", "🚞", "🚝", "🚄", "🚅",
                    "🚈", "🚂", "🚆", "🚇", "🚊", "🚉", "✈️", "🛫", "🛬", "🛩️", "💺", "🚁", "🚟", "🚠", "🚡", "🛰️",
                    "🚀", "🛸", "🛎️", "🧳", "⌛", "⏳", "⌚", "⏰", "⏱️", "⏲️", "🕰️", "🌍", "🌎", "🌏", "🌐", "🗺️",
                    "🧭", "🏔️", "⛰️", "🌋", "🗻", "🏕️", "🏖️", "🏜️", "🏝️", "🏞️", "🏟️", "🏛️", "🏗️", "🧱", "🏘️", "🏚️",
                    "🏠", "🏡", "🏢", "🏣", "🏤", "🏥", "🏦", "🏨", "🏩", "🏪", "🏫", "🏬", "🏭", "🏯", "🏰", "💒",
                    "🗼", "🗽", "⛪", "🕌", "🛕", "🕍", "⛩️", "🕋", "⛲", "⛺", "🌁", "🌃", "🏙️", "🌄", "🌅", "🌆",
                    "🌇", "🌉", "♨️", "🎠", "🎡", "🎢", "💈", "🎪", "🚂", "🚃", "🚄", "🚅", "🚆", "🚇", "🚈", "🚉",
                },
                ["Activities"] = new List<string>
                {
                    "⚽", "🏀", "🏈", "⚾", "🥎", "🎾", "🏐", "🏉", "🥏", "🎱", "🏓", "🏸", "🥅", "🏒", "🏑", "🏏",
                    "🥍", "🏹", "🎣", "🥊", "🥋", "🎽", "🛹", "🛷", "⛸️", "🥌", "🎿", "⛷️", "🏂", "🏋️", "🤼", "🤸",
                    "🤺", "⛹️", "🤾", "🏌️", "🏇", "🧘", "🏄", "🏊", "🚣", "🧗", "🚵", "🚴", "🏆", "🥇", "🥈", "🥉",
                    "🏅", "🎖️", "🏵️", "🎗️", "🎫", "🎟️", "🎪", "🤹", "🎭", "🩰", "🎨", "🎬", "🎤", "🎧", "🎼", "🎹",
                    "🥁", "🎷", "🎺", "🎸", "🪕", "🎻", "🎲", "♟️", "🎯", "🎳", "🎮", "🎰", "🧩",
                },
                ["Objects"] = new List<string>
                {
                    "⌚", "📱", "📲", "💻", "⌨️", "🖥️", "🖨️", "🖱️", "🖲️", "🕹️", "🗜️", "💾", "💿", "📀", "📼", "📷",
                    "📸", "📹", "🎥", "📽️", "🎞️", "📞", "☎️", "📟", "📠", "📺", "📻", "🎙️", "🎚️", "🎛️", "⏱️", "⏲️",
                    "⏰", "🕰️", "⌛", "⏳", "📡", "🔋", "🔌", "💡", "🔦", "🕯️", "🧯", "🛢️", "💸", "💵", "💴", "💶",
                    "💷", "💰", "💳", "💎", "⚖️", "🧰", "🔧", "🔨", "⚒️", "🛠️", "⛏️", "🔩", "⚙️", "🧱", "⛓️", "🧲",
                    "🔫", "💣", "🧨", "🔪", "🗡️", "⚔️", "🛡️", "🚬", "⚰️", "⚱️", "🏺", "🔮", "📿", "🧿", "💈", "⚗️",
                    "🔭", "🔬", "🕳️", "💊", "💉", "🧬", "🦠", "🧫", "🧪", "🌡️", "🧹", "🧺", "🧻", "🚽", "🚿", "🛁",
                    "🛀", "🧼", "🪒", "🧽", "🧴", "🛎️", "🔑", "🗝️", "🚪", "🪑", "🛋️", "🛏️", "🛌", "🧸", "🪆", "🖼️",
                    "🪞", "🪟", "🛍️", "🛒", "🎁", "🎈", "🎏", "🎀", "🪄", "🪅", "🪆", "🧸", "🎊", "🎉", "🎎", "🏮",
                    "🎐", "🧧", "✉️", "📩", "📨", "📧", "💌", "📥", "📤", "📦", "🏷️", "🪧", "📪", "📫", "📬", "📭",
                    "📮", "📯", "📜", "📃", "📄", "📑", "🧾", "📊", "📈", "📉", "🗒️", "🗓️", "📆", "📅", "🗑️", "📇",
                    "🗃️", "🗳️", "🗄️", "📋", "📁", "📂", "🗂️", "🗞️", "📰", "📓", "📔", "📒", "📕", "📗", "📘", "📙",
                    "📚", "📖", "🔖", "🧷", "🔗", "📎", "🖇️", "📐", "📏", "🧮", "📌", "📍", "✂️", "🖊️", "🖋️", "✒️",
                    "🖌️", "🖍️", "📝", "✏️", "🔍", "🔎", "🔏", "🔐", "🔒", "🔓",
                },
                ["Symbols"] = new List<string>
                {
                    "❤️", "🧡", "💛", "💚", "💙", "💜", "🖤", "🤍", "🤎", "💔", "❣️", "💕", "💞", "💓", "💗", "💖",
                    "💘", "💝", "💟", "☮️", "✝️", "☪️", "🕉️", "☸️", "✡️", "🔯", "🕎", "☯️", "☦️", "🛐", "⛎", "♈",
                    "♉", "♊", "♋", "♌", "♍", "♎", "♏", "♐", "♑", "♒", "♓", "🆔", "⚛️", "🉑", "☢️", "☣️", "📴",
                    "📳", "🈶", "🈚", "🈸", "🈺", "🈷️", "✴️", "🆚", "💮", "🉐", "㊙️", "㊗️", "🈴", "🈵", "🈹", "🈲",
                    "🅰️", "🅱️", "🆎", "🆑", "🅾️", "🆘", "❌", "⭕", "🛑", "⛔", "📛", "🚫", "💯", "💢", "♨️", "🚷",
                    "🚯", "🚳", "🚱", "🔞", "📵", "🚭", "❗", "❕", "❓", "❔", "‼️", "⁉️", "🔅", "🔆", "〽️", "⚠️",
                    "🚸", "🔱", "⚜️", "🔰", "♻️", "✅", "🈯", "💹", "❇️", "✳️", "❎", "🌐", "💠", "Ⓜ️", "🌀", "💤",
                    "🏧", "🚾", "♿", "🅿️", "🈳", "🈂️", "🛂", "🛃", "🛄", "🛅", "🚹", "🚺", "🚼", "🚻", "🚮", "🎦",
                    "📶", "🈁", "🔣", "ℹ️", "🔤", "🔡", "🔠", "🆖", "🆗", "🆙", "🆒", "🆕", "🆓", "0️⃣", "1️⃣", "2️⃣",
                    "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟", "🔢", "#️⃣", "*️⃣", "⏏️", "▶️", "⏸️", "⏯️", "⏹️",
                    "⏺️", "⏭️", "⏮️", "⏩", "⏪", "⏫", "⏬", "◀️", "🔼", "🔽", "➡️", "⬅️", "⬆️", "⬇️", "↗️", "↘️",
                    "↙️", "↖️", "↕️", "↔️", "↪️", "↩️", "⤴️", "⤵️", "🔀", "🔁", "🔂", "🔄", "🔃", "🎵", "🎶", "➕",
                    "➖", "➗", "✖️", "💲", "💱", "™️", "©️", "®️", "〰️", "➰", "➿", "🔚", "🔙", "🔛", "🔜", "🔝",
                    "✔️", "☑️", "🔘", "⚪", "⚫", "🔴", "🔵", "🟠", "🟡", "🟢", "🔵", "🟣", "⚫", "⚪", "🟤", "🔶",
                    "🔷", "🔸", "🔹", "🔺", "🔻", "💠", "🔘", "🔳", "🔲", "▪️", "▫️", "◾", "◽", "◼️", "◻️", "🟥",
                    "🟧", "🟨", "🟩", "🟦", "🟪", "⬛", "⬜", "🟫", "🔈", "🔇", "🔉", "🔊", "🔔", "🔕", "📣", "📢",
                    "👁️‍🗨️", "💬", "💭", "🗯️", "♠️", "♣️", "♥️", "♦️", "🃏", "🎴", "🀄", "🕐", "🕑", "🕒", "🕓", "🕔",
                    "🕕", "🕖", "🕗", "🕘", "🕙", "🕚", "🕛", "🕜", "🕝", "🕞", "🕟", "🕠", "🕡", "🕢", "🕣", "🕤",
                    "🕥", "🕦", "🕧",
                },
                ["Flags"] = new List<string>
                {
                    "🏳️", "🏴", "🏁", "🚩", "🏳️‍🌈", "🏳️‍⚧️", "🇦🇫", "🇦🇽", "🇦🇱", "🇩🇿", "🇦🇸", "🇦🇩", "🇦🇴", "🇦🇮", "🇦🇶", "🇦🇬",
                    "🇦🇷", "🇦🇲", "🇦🇼", "🇦🇺", "🇦🇹", "🇦🇿", "🇧🇸", "🇧🇭", "🇧🇩", "🇧🇧", "🇧🇾", "🇧🇪", "🇧🇿", "🇧🇯", "🇧🇲", "🇧🇹",
                    "🇧🇴", "🇧🇦", "🇧🇼", "🇧🇷", "🇮🇴", "🇻🇬", "🇧🇳", "🇧🇬", "🇧🇫", "🇧🇮", "🇰🇭", "🇨🇲", "🇨🇦", "🇮🇨", "🇨🇻", "🇧🇶",
                    "🇰🇾", "🇨🇫", "🇹🇩", "🇨🇱", "🇨🇳", "🇨🇽", "🇨🇨", "🇨🇴", "🇰🇲", "🇨🇬", "🇨🇩", "🇨🇰", "🇨🇷", "🇨🇮", "🇭🇷", "🇨🇺",
                    "🇨🇼", "🇨🇾", "🇨🇿", "🇩🇰", "🇩🇯", "🇩🇲", "🇩🇴", "🇪🇨", "🇪🇬", "🇸🇻", "🇬🇶", "🇪🇷", "🇪🇪", "🇪🇹", "🇪🇺", "🇫🇰",
                    "🇫🇴", "🇫🇯", "🇫🇮", "🇫🇷", "🇬🇫", "🇵🇫", "🇹🇫", "🇬🇦", "🇬🇲", "🇬🇪", "🇩🇪", "🇬🇭", "🇬🇮", "🇬🇷", "🇬🇱", "🇬🇩",
                    "🇬🇵", "🇬🇺", "🇬🇹", "🇬🇬", "🇬🇳", "🇬🇼", "🇬🇾", "🇭🇹", "🇭🇳", "🇭🇰", "🇭🇺", "🇮🇸", "🇮🇳", "🇮🇩", "🇮🇷", "🇮🇶",
                    "🇮🇪", "🇮🇲", "🇮🇱", "🇮🇹", "🇯🇲", "🇯🇵", "🎌", "🇯🇪", "🇯🇴", "🇰🇿", "🇰🇪", "🇰🇮", "🇽🇰", "🇰🇼", "🇰🇬", "🇱🇦",
                    "🇱🇻", "🇱🇧", "🇱🇸", "🇱🇷", "🇱🇾", "🇱🇮", "🇱🇹", "🇱🇺", "🇲🇴", "🇲🇰", "🇲🇬", "🇲🇼", "🇲🇾", "🇲🇻", "🇲🇱", "🇲🇹",
                    "🇲🇭", "🇲🇶", "🇲🇷", "🇲🇺", "🇾🇹", "🇲🇽", "🇫🇲", "🇲🇩", "🇲🇨", "🇲🇳", "🇲🇪", "🇲🇸", "🇲🇦", "🇲🇿", "🇲🇲", "🇳🇦",
                    "🇳🇷", "🇳🇵", "🇳🇱", "🇳🇨", "🇳🇿", "🇳🇮", "🇳🇪", "🇳🇬", "🇳🇺", "🇳🇫", "🇰🇵", "🇲🇵", "🇳🇴", "🇴🇲", "🇵🇰", "🇵🇼",
                    "🇵🇸", "🇵🇦", "🇵🇬", "🇵🇾", "🇵🇪", "🇵🇭", "🇵🇳", "🇵🇱", "🇵🇹", "🇵🇷", "🇶🇦", "🇷🇪", "🇷🇴", "🇷🇺", "🇷🇼", "🇼🇸",
                    "🇸🇲", "🇸🇦", "🇸🇳", "🇷🇸", "🇸🇨", "🇸🇱", "🇸🇬", "🇸🇽", "🇸🇰", "🇸🇮", "🇬🇸", "🇸🇧", "🇸🇴", "🇿🇦", "🇰🇷", "🇸🇸",
                    "🇪🇸", "🇱🇰", "🇧🇱", "🇸🇭", "🇰🇳", "🇱🇨", "🇵🇲", "🇻🇨", "🇸🇩", "🇸🇷", "🇸🇿", "🇸🇪", "🇨🇭", "🇸🇾", "🇹🇼", "🇹🇯",
                    "🇹🇿", "🇹🇭", "🇹🇱", "🇹🇬", "🇹🇰", "🇹🇴", "🇹🇹", "🇹🇳", "🇹🇷", "🇹🇲", "🇹🇨", "🇹🇻", "🇺🇬", "🇺🇦", "🇦🇪", "🇬🇧",
                    "🇺🇸", "🇻🇮", "🇺🇾", "🇺🇿", "🇻🇺", "🇻🇦", "🇻🇪", "🇻🇳", "🇼🇫", "🇪🇭", "🇾🇪", "🇿🇲", "🇿🇼",
                },
            };
        }

        private void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string category)
            {
                this.SelectCategory(category);
            }
        }

        private void SelectCategory(string category)
        {
            this.selectedCategory = category;
            this.UpdateEmojiList();
            this.UpdateCategoryButtons();
        }

        private void UpdateCategoryButtons()
        {
            var buttons = new[]
            {
                this.RecentButton, this.SmileysButton, this.PeopleButton, this.AnimalsButton,
                this.FoodButton, this.TravelButton, this.ActivitiesButton, this.ObjectsButton,
                this.SymbolsButton, this.FlagsButton,
            };

            foreach (var button in buttons)
            {
                if (button.Tag?.ToString() == this.selectedCategory)
                {
                    button.Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromArgb(100, 255, 255, 255));
                }
                else
                {
                    button.Background = System.Windows.Media.Brushes.Transparent;
                }
            }
        }

        private void UpdateEmojiList()
        {
            this.EmojiList.Clear();

            List<string> emojisToShow;

            if (this.selectedCategory == "Recent")
            {
                emojisToShow = this.recentEmojis.ToList();
            }
            else if (!string.IsNullOrWhiteSpace(this.SearchText))
            {
                // Search across all categories (simple contains check for emoji characters)
                emojisToShow = this.emojiCategories.Values
                    .SelectMany(x => x)
                    .Where(e => e.IndexOf(this.SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                this.SearchText.IndexOf(e, StringComparison.OrdinalIgnoreCase) >= 0)
                    .Distinct()
                    .ToList();
            }
            else if (this.emojiCategories.TryGetValue(this.selectedCategory, out var categoryEmojis))
            {
                emojisToShow = categoryEmojis;
            }
            else
            {
                emojisToShow = new List<string>();
            }

            foreach (var emoji in emojisToShow)
            {
                this.EmojiList.Add(emoji);
            }
        }

        private void EmojiButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string? emoji = null;

                // Try to get from DataContext (binding) - this is the most reliable way
                if (button.DataContext is string dataContextString)
                {
                    emoji = dataContextString;
                }

                // Try to get from TextBlock (including Emoji.Wpf.TextBlock)
                else if (button.Content is System.Windows.Controls.TextBlock textBlock)
                {
                    // Emoji.Wpf.TextBlock uses Text property directly, no Run needed
                    if (!string.IsNullOrEmpty(textBlock.Text))
                    {
                        emoji = textBlock.Text;
                    }

                    // Fallback: try to get from Run if present (for compatibility)
                    else if (textBlock.Inlines != null && textBlock.Inlines.Count > 0)
                    {
                        var run = textBlock.Inlines.OfType<System.Windows.Documents.Run>().FirstOrDefault();
                        if (run != null && !string.IsNullOrEmpty(run.Text))
                        {
                            emoji = run.Text;
                        }
                    }
                }

                // Fallback: try to get from Content directly
                else if (button.Content is string contentString)
                {
                    emoji = contentString;
                }

                if (!string.IsNullOrEmpty(emoji))
                {
                    this.AddToRecent(emoji);
                    this.EmojiSelected?.Invoke(this, emoji);
                }
            }
        }

        private void AddToRecent(string emoji)
        {
            // Remove if already exists
            this.recentEmojis.Remove(emoji);

            // Add to front
            this.recentEmojis.Insert(0, emoji);

            // Keep only max recent
            if (this.recentEmojis.Count > MaxRecentEmojis)
            {
                this.recentEmojis.RemoveRange(MaxRecentEmojis, this.recentEmojis.Count - MaxRecentEmojis);
            }

            this.SaveRecentEmojis();
        }

        private List<string> LoadRecentEmojis()
        {
            try
            {
                var appDataPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "NexusTeam",
                    "recent_emojis.txt");

                if (System.IO.File.Exists(appDataPath))
                {
                    var content = System.IO.File.ReadAllText(appDataPath);
                    return content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .ToList();
                }
            }
            catch
            {
                // Ignore errors
            }

            return new List<string>();
        }

        private void SaveRecentEmojis()
        {
            try
            {
                var appDataPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "NexusTeam");

                if (!System.IO.Directory.Exists(appDataPath))
                {
                    System.IO.Directory.CreateDirectory(appDataPath);
                }

                var filePath = System.IO.Path.Combine(appDataPath, "recent_emojis.txt");
                System.IO.File.WriteAllText(filePath, string.Join("\n", this.recentEmojis));
            }
            catch
            {
                // Ignore errors
            }
        }

        /// <summary>
        /// Event raised when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
