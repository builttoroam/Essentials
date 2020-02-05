using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using PropertyChangingEventArgs = System.ComponentModel.PropertyChangingEventArgs;

namespace Samples.ViewModel
{
    public static class RecurrenceEndType
    {
        public const string Indefinitely = "Indefinitely";

        public const string AfterOccurences = "After a set number of times";

        public const string UntilEndDate = "Continues until a specified date";
    }

    public class CalendarDayOfWeekSwitch : INotifyPropertyChanged
    {
        public CalendarDayOfWeekSwitch(CalendarDayOfWeek val, PropertyChangedEventHandler propertyChangedCallBack)
        {
            Day = val;
            PropertyChanged += propertyChangedCallBack;
        }

        bool isChecked;

        public CalendarDayOfWeek Day { get; set; }

        public override string ToString() => Day.ToString();

        public bool IsChecked
        {
            get => isChecked;
            set
            {
                isChecked = value;
                OnPropertyChanged(nameof(PropertyChanged.Method.Name));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName]string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class CalendarEventAddViewModel : BaseViewModel
    {
        public CalendarEventAddViewModel(string calendarId, string calendarName, CalendarEvent existingEvent = null)
        {
            CalendarId = calendarId;
            CalendarName = calendarName;
            recurrenceDays = new ObservableCollection<CalendarDayOfWeekSwitch>()
            {
                new CalendarDayOfWeekSwitch(CalendarDayOfWeek.Sunday, OnChildCheckBoxChangedEvent),
                new CalendarDayOfWeekSwitch(CalendarDayOfWeek.Monday, OnChildCheckBoxChangedEvent),
                new CalendarDayOfWeekSwitch(CalendarDayOfWeek.Tuesday, OnChildCheckBoxChangedEvent),
                new CalendarDayOfWeekSwitch(CalendarDayOfWeek.Wednesday, OnChildCheckBoxChangedEvent),
                new CalendarDayOfWeekSwitch(CalendarDayOfWeek.Thursday, OnChildCheckBoxChangedEvent),
                new CalendarDayOfWeekSwitch(CalendarDayOfWeek.Friday, OnChildCheckBoxChangedEvent),
                new CalendarDayOfWeekSwitch(CalendarDayOfWeek.Saturday, OnChildCheckBoxChangedEvent)
            };

            if (existingEvent != null)
            {
                EventId = existingEvent.Id;
                EventTitle = existingEvent.Title;
                Description = existingEvent.Description;
                EventLocation = existingEvent.Location;
                Url = existingEvent.Url;
                AllDay = existingEvent.AllDay;
                StartDate = existingEvent.StartDate.Date;
                EndDate = existingEvent.EndDate?.Date ?? existingEvent.StartDate.Date;
                StartTime = existingEvent.StartDate.TimeOfDay;
                EndTime = existingEvent.EndDate?.TimeOfDay ?? existingEvent.StartDate.TimeOfDay;
                if (existingEvent.RecurrancePattern != null)
                {
                    SelectedRecurrenceType = existingEvent.RecurrancePattern.Frequency;
                    RecurrenceInterval = existingEvent.RecurrancePattern.Interval;

                    // var selectedDays = existingEvent.RecurrancePattern.DaysOfTheWeek != null && existingEvent.RecurrancePattern.DaysOfTheWeek.Any() ? new ObservableCollection<CalendarDayOfWeekSwitch>(existingEvent.RecurrancePattern.DaysOfTheWeek.ConvertAll(x => new CalendarDayOfWeekSwitch() { Day = x, IsChecked = true }).ToList()) : new ObservableCollection<CalendarDayOfWeekSwitch>();
                    // foreach (var r in selectedDays)
                    // {
                    //     RecurrenceDays.FirstOrDefault(x => x.Day == r.Day).IsChecked = true;
                    // }
                    switch (existingEvent.RecurrancePattern.Frequency)
                    {
                        case RecurrenceFrequency.MonthlyOnDay:
                        case RecurrenceFrequency.YearlyOnDay:
                            SelectedRecurrenceType = SelectedRecurrenceType == RecurrenceFrequency.MonthlyOnDay ? RecurrenceFrequency.Monthly : RecurrenceFrequency.Yearly;
                            IsMonthDaySpecific = false;
                            SelectedRecurrenceMonthWeek = existingEvent.RecurrancePattern.WeekOfMonth;
                            SelectedMonthWeekRecurrenceDay = existingEvent.RecurrancePattern.DaysOfTheWeek.FirstOrDefault();
                            break;
                        case RecurrenceFrequency.Monthly:
                        case RecurrenceFrequency.Yearly:
                            SelectedRecurrenceMonthDay = existingEvent.RecurrancePattern.DayOfTheMonth;
                            break;
                    }
                    if (existingEvent.RecurrancePattern.MonthOfTheYear.HasValue)
                    {
                        SelectedRecurrenceYearlyMonth = existingEvent.RecurrancePattern.MonthOfTheYear.Value;
                    }
                    if (existingEvent.RecurrancePattern.EndDate.HasValue)
                    {
                        RecurrenceEndDate = existingEvent.RecurrancePattern.EndDate.Value.DateTime;
                        SelectedRecurrenceEndType = RecurrenceEndType.UntilEndDate;
                    }
                    else if (existingEvent.RecurrancePattern.TotalOccurrences.HasValue)
                    {
                        RecurrenceEndInterval = existingEvent.RecurrancePattern.TotalOccurrences.Value;
                        SelectedRecurrenceEndType = RecurrenceEndType.AfterOccurences;
                    }
                    else
                    {
                        RecurrenceEndDate = null;
                        SelectedRecurrenceEndType = RecurrenceEndType.Indefinitely;
                    }
                }
            }
            CreateOrUpdateEvent = new Command(CreateOrUpdateCalendarEvent);
        }

        public string EventId { get; set; }

        public string CalendarId { get; set; }

        public string CalendarName { get; set; }

        string eventTitle;

        public string EventTitle
        {
            get => eventTitle;
            set
            {
                if (SetProperty(ref eventTitle, value))
                {
                    OnPropertyChanged(nameof(CanCreateOrUpdateEvent));
                }
            }
        }

        public string EventActionText => string.IsNullOrEmpty(EventId) ? "Add Event" : "Update Event";

        public bool CanCreateOrUpdateEvent => !string.IsNullOrWhiteSpace(EventTitle)
            && ((EndDate.Date == StartDate.Date && (EndTime > StartTime || AllDay)) || EndDate.Date > StartDate.Date)
            && (!CanAlterRecurrence || StartDate < RecurrenceEndDate || SelectedRecurrenceEndType == RecurrenceEndType.Indefinitely || (RecurrenceEndInterval.HasValue && RecurrenceEndInterval.Value > 0))
            && IsValidUrl(Url);

        string description;

        public string Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }

        string url;

        public string Url
        {
            get => url;
            set
            {
                if (SetProperty(ref url, value))
                {
                    OnPropertyChanged(nameof(CanCreateOrUpdateEvent));
                }
            }
        }

        public bool IsValidUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return true;
            }
            else if (!Regex.IsMatch(url, @"^https?:\/\/", RegexOptions.IgnoreCase))
            {
                url = "http://" + url;
                Url = url;
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out var uriResult))
            {
                return uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps;
            }

            return false;
        }

        string eventLocation;

        public string EventLocation
        {
            get => eventLocation;
            set => SetProperty(ref eventLocation, value);
        }

        bool allDay;

        public bool AllDay
        {
            get => allDay;
            set
            {
                if (SetProperty(ref allDay, value))
                {
                    OnPropertyChanged(nameof(CanCreateOrUpdateEvent));
                    OnPropertyChanged(nameof(DisplayTimeInformation));
                }
            }
        }

        DateTime startDate = DateTime.Now.Date;

        public DateTime StartDate
        {
            get => startDate;
            set
            {
                if (SetProperty(ref startDate, value))
                {
                    OnPropertyChanged(nameof(CanCreateOrUpdateEvent));
                }
            }
        }

        TimeSpan startTime = DateTime.Now.AddHours(1).TimeOfDay.RoundToNearestMinutes(30);

        public TimeSpan StartTime
        {
            get => startTime;
            set
            {
                if (SetProperty(ref startTime, value))
                {
                    OnPropertyChanged(nameof(CanCreateOrUpdateEvent));
                }
            }
        }

        DateTime endDate = DateTime.Now.Date;

        public DateTime EndDate
        {
            get => endDate;
            set
            {
                if (SetProperty(ref endDate, value))
                {
                    OnPropertyChanged(nameof(CanCreateOrUpdateEvent));
                }
            }
        }

        TimeSpan endTime = DateTime.Now.AddHours(2).TimeOfDay.RoundToNearestMinutes(30);

        public TimeSpan EndTime
        {
            get => endTime;
            set
            {
                if (SetProperty(ref endTime, value))
                {
                    OnPropertyChanged(nameof(CanCreateOrUpdateEvent));
                }
            }
        }

        public bool DisplayTimeInformation => !AllDay && !CanAlterRecurrence;

        // Recurrence Setup
        public bool CanAlterRecurrence => SelectedRecurrenceType != null;

        public bool IsDaily => SelectedRecurrenceType == RecurrenceFrequency.Daily;

        public bool IsWeekly => SelectedRecurrenceType == RecurrenceFrequency.Weekly;

        public bool IsMonthlyOrYearly => SelectedRecurrenceType == RecurrenceFrequency.MonthlyOnDay || SelectedRecurrenceType == RecurrenceFrequency.Monthly || SelectedRecurrenceType == RecurrenceFrequency.YearlyOnDay || SelectedRecurrenceType == RecurrenceFrequency.Yearly;

        public bool IsYearly => SelectedRecurrenceType == RecurrenceFrequency.Yearly || SelectedRecurrenceType == RecurrenceFrequency.YearlyOnDay;

        public enum DayOfWeekGroup
        {
            None = 0,
            Weekday = 62,
            Weekend = 65,
            Alldays = 127,
            Custom
        }

        public List<DayOfWeekGroup> RecurrenceDayOfWeekGroup { get; } = new List<DayOfWeekGroup>()
        {
            DayOfWeekGroup.None,
            DayOfWeekGroup.Weekday,
            DayOfWeekGroup.Weekend,
            DayOfWeekGroup.Alldays,
            DayOfWeekGroup.Custom
        };

        DayOfWeekGroup? selectedRecurrenceDayOfWeekGroup = DayOfWeekGroup.None;

        public DayOfWeekGroup? SelectedRecurrenceDayOfWeekGroup
        {
            get
            {
                var result = RecurrenceDays.Where(x => x.IsChecked).Sum(x => (int)x.Day);
                switch (result)
                {
                    case (int)DayOfWeekGroup.None:
                        return DayOfWeekGroup.None;
                    case (int)DayOfWeekGroup.Weekday:
                        return DayOfWeekGroup.Weekday;
                    case (int)DayOfWeekGroup.Weekend:
                        return DayOfWeekGroup.Weekend;
                    case (int)DayOfWeekGroup.Alldays:
                        return DayOfWeekGroup.Alldays;
                    default:
                        return DayOfWeekGroup.Custom;
                }
            }

            set
            {
                if (SetProperty(ref selectedRecurrenceDayOfWeekGroup, value))
                {
                    if (value == DayOfWeekGroup.Weekday)
                    {
                        SetCheckBoxes((int)DayOfWeekGroup.Weekday);
                    }
                    else if (value == DayOfWeekGroup.Weekend)
                    {
                        SetCheckBoxes((int)DayOfWeekGroup.Weekend);
                    }
                    else if (value == DayOfWeekGroup.Alldays)
                    {
                        SetCheckBoxes((int)DayOfWeekGroup.Alldays);
                    }
                    else if (value == DayOfWeekGroup.None)
                    {
                        SetCheckBoxes((int)DayOfWeekGroup.None);
                    }
                }
            }
        }

        bool isMonthDaySpecific = true;

        public bool IsMonthDaySpecific
        {
            get => isMonthDaySpecific;
            set
            {
                if (SetProperty(ref isMonthDaySpecific, value))
                {
                    if (value)
                    {
                        SelectedRecurrenceMonthDay = 1;
                        SelectedRecurrenceMonthWeek = null;
                        SelectedMonthWeekRecurrenceDay = null;
                    }
                    else
                    {
                        SelectedRecurrenceMonthDay = 0;
                        SelectedRecurrenceMonthWeek = IterationOffset.First;
                        SelectedMonthWeekRecurrenceDay = CalendarDayOfWeek.Monday;
                    }
                }
            }
        }

        void SetCheckBoxes(int bitFlagValue)
        {
            var currentVal = bitFlagValue;
            var maxValue = (int)CalendarDayOfWeek.Saturday;
            for (var i = maxValue; i > 0; i /= 2)
            {
                // RecurrenceDays[(int)Math.Log(i, 2)].TriggerPropertyChanged = false;
                RecurrenceDays[(int)Math.Log(i, 2)].IsChecked = currentVal >= i;

                // RecurrenceDays[(int)Math.Log(i, 2)].TriggerPropertyChanged = true;

                if (currentVal >= i)
                {
                    currentVal -= i;
                }
            }
            OnPropertyChanged(nameof(RecurrenceDays));
        }

        void OnChildCheckBoxChangedEvent(object sender, PropertyChangedEventArgs e)
        {
            if (sender == null)
            {
                return;
            }
            if (!(sender is CalendarDayOfWeekSwitch calendarDayOfWeek))
            {
                return;
            }

            OnPropertyChanged(nameof(SelectedRecurrenceDayOfWeekGroup));
        }

        private ObservableCollection<CalendarDayOfWeekSwitch> recurrenceDays;

        public ObservableCollection<CalendarDayOfWeekSwitch> RecurrenceDays
        {
            get => recurrenceDays;
            set
            {
                if (value != null)
                {
                    recurrenceDays = value;
                }
            }
        }

        public List<RecurrenceFrequency> RecurrenceTypes { get; } = new List<RecurrenceFrequency>()
        {
            RecurrenceFrequency.Daily,
            RecurrenceFrequency.Weekly,
            RecurrenceFrequency.Monthly,
            RecurrenceFrequency.Yearly
        };

        RecurrenceFrequency? selectedRecurrenceType = null;

        public RecurrenceFrequency? SelectedRecurrenceType
        {
            get => selectedRecurrenceType;

            set
            {
                if (SetProperty(ref selectedRecurrenceType, value))
                {
                    OnPropertyChanged(nameof(CanAlterRecurrence));
                    OnPropertyChanged(nameof(DisplayTimeInformation));
                    OnPropertyChanged(nameof(IsDaily));
                    OnPropertyChanged(nameof(IsWeekly));
                    OnPropertyChanged(nameof(IsMonthlyOrYearly));
                    OnPropertyChanged(nameof(IsYearly));
                    OnPropertyChanged(nameof(SelectedRecurrenceTypeDisplay));
                }
            }
        }

        public string SelectedRecurrenceTypeDisplay => SelectedRecurrenceType.ToString().Replace("ily", "yly").Replace("ly", "(s)");

        uint recurrenceInterval = 1;

        public uint RecurrenceInterval
        {
            get => recurrenceInterval;
            set => SetProperty(ref recurrenceInterval, value);
        }

        public ObservableCollection<uint> RecurrenceMonthDay { get; set; } = new ObservableCollection<uint>()
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31
        };

        uint selectedRecurrenceMonthDay = 1;

        public uint SelectedRecurrenceMonthDay
        {
            get => selectedRecurrenceMonthDay;
            set
            {
                if (SetProperty(ref selectedRecurrenceMonthDay, value))
                {
                    if (value != 0)
                    {
                        IsMonthDaySpecific = true;
                    }
                }
            }
        }

        public List<IterationOffset> RecurrenceMonthWeek { get; } = new List<IterationOffset>()
        {
            IterationOffset.First,
            IterationOffset.Second,
            IterationOffset.Third,
            IterationOffset.Fourth,
            IterationOffset.Last
        };

        IterationOffset? selectedRecurrenceMonthWeek = null;

        public IterationOffset? SelectedRecurrenceMonthWeek
        {
            get => selectedRecurrenceMonthWeek;
            set => SetProperty(ref selectedRecurrenceMonthWeek, value);
        }

        public List<CalendarDayOfWeek> MonthWeekRecurrenceDay { get; set; } = new List<CalendarDayOfWeek>()
        {
            CalendarDayOfWeek.Monday,
            CalendarDayOfWeek.Tuesday,
            CalendarDayOfWeek.Wednesday,
            CalendarDayOfWeek.Thursday,
            CalendarDayOfWeek.Friday,
            CalendarDayOfWeek.Saturday,
            CalendarDayOfWeek.Sunday
        };

        CalendarDayOfWeek? selectedMonthWeekRecurrenceDay = null;

        public CalendarDayOfWeek? SelectedMonthWeekRecurrenceDay
        {
            get => selectedMonthWeekRecurrenceDay;
            set => SetProperty(ref selectedMonthWeekRecurrenceDay, value);
        }

        public List<MonthOfYear> RecurrenceYearlyMonth { get; } = new List<MonthOfYear>()
        {
            MonthOfYear.January,
            MonthOfYear.February,
            MonthOfYear.March,
            MonthOfYear.April,
            MonthOfYear.May,
            MonthOfYear.June,
            MonthOfYear.July,
            MonthOfYear.August,
            MonthOfYear.September,
            MonthOfYear.October,
            MonthOfYear.November,
            MonthOfYear.December
        };

        MonthOfYear selectedRecurrenceYearlyMonth = MonthOfYear.January;

        public MonthOfYear SelectedRecurrenceYearlyMonth
        {
            get => selectedRecurrenceYearlyMonth;
            set
            {
                if (SetProperty(ref selectedRecurrenceYearlyMonth, value))
                {
                    var days = Enumerable.Range(1, DateTime.DaysInMonth(StartDate.Year, (int)value)).Select(x => (uint)x).ToList();
                    RecurrenceMonthDay.Clear();
                    days.ForEach(x => RecurrenceMonthDay.Add(x));
                }
            }
        }

        public ObservableCollection<string> RecurrenceEndTypes { get; } = new ObservableCollection<string>()
        {
            RecurrenceEndType.Indefinitely,
            RecurrenceEndType.AfterOccurences,
            RecurrenceEndType.UntilEndDate
        };

        string selectedRecurrenceEndType = "Indefinitely";

        public string SelectedRecurrenceEndType
        {
            get => selectedRecurrenceEndType;

            set
            {
                if (SetProperty(ref selectedRecurrenceEndType, value) && selectedRecurrenceEndType != null)
                {
                    switch (value)
                    {
                        case RecurrenceEndType.Indefinitely:
                            RecurrenceEndDate = null;
                            RecurrenceEndInterval = null;
                            break;
                        case RecurrenceEndType.AfterOccurences:
                            RecurrenceEndDate = null;
                            RecurrenceEndInterval = !RecurrenceEndInterval.HasValue ? 1 : RecurrenceEndInterval;
                            break;
                        case RecurrenceEndType.UntilEndDate:
                            RecurrenceEndInterval = null;
                            RecurrenceEndDate = !RecurrenceEndDate.HasValue ? DateTime.Now.AddMonths(6) : RecurrenceEndDate;
                            break;
                        default:
                            RecurrenceEndDate = null;
                            RecurrenceEndInterval = null;
                            break;
                    }
                    OnPropertyChanged(nameof(CanCreateOrUpdateEvent));
                }
            }
        }

        DateTime? recurrenceEndDate;

        public DateTime? RecurrenceEndDate
        {
            get => recurrenceEndDate;
            set
            {
                if (SetProperty(ref recurrenceEndDate, value))
                {
                    OnPropertyChanged(nameof(CanCreateOrUpdateEvent));
                }
            }
        }

        uint? recurrenceEndInterval;

        public uint? RecurrenceEndInterval
        {
            get => recurrenceEndInterval;
            set
            {
                if (SetProperty(ref recurrenceEndInterval, value))
                {
                    OnPropertyChanged(nameof(CanCreateOrUpdateEvent));
                }
            }
        }

        public ICommand CreateOrUpdateEvent { get; }

        async void CreateOrUpdateCalendarEvent()
        {
            var startDto = new DateTimeOffset(StartDate + StartTime);
            var endDto = new DateTimeOffset(EndDate + EndTime);
            var newEvent = new CalendarEvent()
            {
                Id = EventId,
                CalendarId = CalendarId,
                Title = EventTitle,
                AllDay = AllDay,
                Description = Description,
                Location = EventLocation,
                Url = url,
                StartDate = startDto,
                EndDate = !AllDay ? !CanAlterRecurrence ? (DateTimeOffset?)endDto : new DateTimeOffset(StartDate + EndTime) : null
            };

            if (CanAlterRecurrence)
            {
                List<CalendarDayOfWeek> daysOfTheWeek = null;
                var selectedFrequency = SelectedRecurrenceType;
                IterationOffset? dayIterationOffset = null;
                switch (selectedFrequency)
                {
                    case RecurrenceFrequency.Daily:
                    case RecurrenceFrequency.Weekly:
                        daysOfTheWeek = RecurrenceDays.Where(x => x.IsChecked).ToList().ConvertAll(x => x.Day);
                        break;

                    default:
                        if (SelectedMonthWeekRecurrenceDay.HasValue)
                        {
                            daysOfTheWeek = new List<CalendarDayOfWeek>()
                            {
                                SelectedMonthWeekRecurrenceDay.Value
                            };
                        }

                        if (SelectedRecurrenceMonthWeek != null)
                        {
                            dayIterationOffset = SelectedRecurrenceMonthWeek;
                        }
                        break;
                }

                newEvent.RecurrancePattern = new RecurrenceRule()
                {
                    Frequency = selectedFrequency,
                    Interval = RecurrenceInterval,
                    DaysOfTheWeek = daysOfTheWeek,
                    DayOfTheMonth = SelectedRecurrenceMonthDay,
                    WeekOfMonth = dayIterationOffset,
                    MonthOfTheYear = SelectedRecurrenceYearlyMonth,
                    EndDate = RecurrenceEndDate,
                    TotalOccurrences = RecurrenceEndInterval
                };
            }

            if (string.IsNullOrEmpty(EventId))
            {
                var eventId = await Calendars.CreateCalendarEvent(newEvent);
                await DisplayAlertAsync("Created event id: " + eventId);
            }
            else
            {
                if (await Calendars.UpdateCalendarEvent(newEvent))
                {
                    await DisplayAlertAsync("Updated event id: " + newEvent.Id);
                }
            }
        }
    }
}
