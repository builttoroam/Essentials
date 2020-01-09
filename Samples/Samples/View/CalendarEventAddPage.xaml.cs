using System;
using System.Collections.ObjectModel;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Samples.View
{
    public partial class CalendarEventAddPage : BasePage
    {
        public CalendarEventAddPage()
        {
            InitializeComponent();
        }

        int maxIntervalLength = 2;

        void Entry_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender == null || !(sender is Entry entry))
            {
                return;
            }

            var curEntryText = entry.Text;
            if (curEntryText.Length > maxIntervalLength)
            {
                curEntryText = curEntryText.Remove(curEntryText.Length - 1);
                entry.Text = curEntryText;
            }
        }

        void RecurrenceEndIntervalEntry_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender == null || !(sender is Entry entry))
            {
                return;
            }

            var curEntryText = entry.Text;
            if (curEntryText != null && curEntryText.Length > maxIntervalLength)
            {
                curEntryText = curEntryText.Remove(curEntryText.Length - 1);
                entry.Text = curEntryText;
            }
        }
    }
}
