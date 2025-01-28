using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using System.ComponentModel;

namespace HybridApp.Manager
{
    public class IconSettingsViewModel
    {
        public ObservableCollection<SettingItemBase> Settings { get; set; }

        public IconSettingsViewModel()
        {
            Settings = new ObservableCollection<SettingItemBase> {
                new IconTypeSettingItem
                {
                    Options = new ObservableCollection<string> {
                        "WebsiteFavicon",
                        "GoogleAPI",
                        "CustomIcon"
                    },
                    SelectedIndex = 0,
                    ImageSource = null,
                    ImageShown = Visibility.Collapsed,
                    PlaceholderShown = Visibility.Visible
                }
            };
        }
    }

    public class GeneralSettingsViewModel 
    {
        public ObservableCollection<SettingItemBase> Settings { get; set; }

        public void SetSettingData(string ID, string value)
        {
            for (int i = 0; i < this.Settings.Count(); i++)
            {
                SettingItemBase sib = this.Settings[i];
                if (sib.ID == ID)
                {
                    var cb = sib as ComboBoxSettingItem;
                    var ss = sib as StringSettingItem;

                    if (cb is not null)
                    {
                        cb.SelectedOption = value;
                        this.Settings[i] = cb;
                    }
                    else if (ss is not null)
                    {
                        ss.Value = value;
                        this.Settings[i] = ss;
                    }
                }
            }
        }

        public void SetSettingData(string ID, bool value)
        {
            for (int i = 0; i < this.Settings.Count(); i++)
            {
                SettingItemBase sib = this.Settings[i];
                if (sib.ID == ID)
                {
                    var bs = sib as BooleanSettingItem;

                    if (bs is not null)
                    {
                        bs.IsEnabled = value;
                        this.Settings[i] = bs;
                    }
                }
            }
        }

        public GeneralSettingsViewModel()
        {
            Settings = new ObservableCollection<SettingItemBase>
        {
            new StringSettingItem
            {
                ID = "WebsiteName",
                Description = "Website Name",
                Placeholder ="Website name...",
                Value = ""
            },
            new StringSettingItem
            {
                ID = "WebsiteURL",
                Description = "Website URL",
                Placeholder = "Website URL...",
                Value = ""
            },
            new BooleanSettingItem
            {
                ID = "StartMenuShortcut",
                Description = "Show Start Menu shortcut",
                IsEnabled = false
            },
            new BooleanSettingItem
            {
                ID = "DesktopShortcut",
                Description = "Show desktop shortcut",
                IsEnabled = false
            }
        };
        }
    }

    public partial class SettingTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? BooleanTemplate { get; set; }
        public DataTemplate? StringTemplate { get; set; }
        public DataTemplate? ComboBoxTemplate { get; set; }
        public DataTemplate? IconTypeTemplate { get; set; }

        protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
        {
            return item switch
            {
                BooleanSettingItem => BooleanTemplate,
                StringSettingItem => StringTemplate,
                ComboBoxSettingItem => ComboBoxTemplate,
                IconTypeSettingItem => IconTypeTemplate,
                _ => null,
            };
        }
    }


    public abstract class SettingItemBase
    {
        public string? ID { get; set; }
        public string? Description { get; set; }
    }

    public class BooleanSettingItem : SettingItemBase
    {
        public bool IsEnabled { get; set; }
    }

    public class StringSettingItem : SettingItemBase
    {
        public string? Placeholder { get; set; }
        public string? Value { get; set; }
    }

    public class ComboBoxSettingItem : SettingItemBase
    {
        public ObservableCollection<string>? Options { get; set; }
        public string? SelectedOption { get; set; }
    }

    public class IconTypeSettingItem : SettingItemBase, INotifyPropertyChanged
    {
        private ObservableCollection<string>? options;
        public ObservableCollection<string>? Options
        {
            get => options;
            set
            {
                if (options != value)
                {
                    options = value;
                    OnPropertyChanged(nameof(Options));
                }
            }
        }

        private int selectedIndex;
        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                if (selectedIndex != value)
                {
                    selectedIndex = value;
                    OnPropertyChanged(nameof(SelectedIndex));
                }
            }
        }

        private ImageSource? imageSource;
        public ImageSource? ImageSource
        {
            get => imageSource;
            set
            {
                if (imageSource != value)
                {
                    imageSource = value;
                    OnPropertyChanged(nameof(ImageSource));
                }
            }
        }

        private Visibility imageShown;
        public Visibility ImageShown
        {
            get => imageShown;
            set
            {
                if (imageShown != value)
                {
                    imageShown = value;
                    OnPropertyChanged(nameof(ImageShown));
                }
            }
        }

        private Visibility placeholderShown;
        public Visibility PlaceholderShown
        {
            get => placeholderShown;
            set
            {
                if (placeholderShown != value)
                {
                    placeholderShown = value;
                    OnPropertyChanged(nameof(PlaceholderShown));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
