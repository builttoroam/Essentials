using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Samples.ViewModel
{
    public static class RecurrenceEndType
    {
        public const string Indefinitely = "Indefinitely";

        public const string AfterOccurences = "After a set number of times";

        public const string UntilEndDate = "Continues until a specified date";
    }

    public class DayOfTheWeekSwitch : INotifyPropertyChanged
    {
        bool isChecked;

        public DayOfTheWeek Day { get; set; }

        public override string ToString() => Day.ToString();

        public bool IsChecked
        {
            get => isChecked;
            set
            {
                isChecked = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName]string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class CalendarEventAddViewModel : BaseViewModel
    {
        static readonly ObservableCollection<DayOfTheWeekSwitch> recurrenceWeekdays = new ObservableCollection<DayOfTheWeekSwitch>()
        {
            new DayOfTheWeekSwitch() { Day = DayOfTheWeek.Monday, IsChecked = true },
            new DayOfTheWeekSwitch() { Day = DayOfTheWeek.Tuesday, IsChecked = true },
            new DayOfTheWeekSwitch() { Day = DayOfTheWeek.Wednesday, IsChecked = true },
            new DayOfTheWeekSwitch() { Day = DayOfTheWeek.Thursday, IsChecked = true },
            new DayOfTheWeekSwitch() { Day = DayOfTheWeek.Friday, IsChecked = true },
            new DayOfTheWeekSwitch() { Day = DayOfTheWeek.Saturday, IsChecked = false },
            new DayOfTheWeekSwitch() { Day = DayOfTheWeek.Sunday, IsChecked = false }
        };

        public CalendarEventAddViewModel(string calendarId, string calendarName, DeviceEvent existingEvent = null)
        {
            CalendarId = calendarId;
            CalendarName = calendarName;

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
                    SelectedRecurrenceType = existingEvent.RecurrancePattern.Frequency.ToString();
                    RecurrenceInterval = existingEvent.RecurrancePattern.Interval;
                    var selectedDays = existingEvent.RecurrancePattern.DaysOfTheWeek != null && existingEvent.RecurrancePattern.DaysOfTheWeek.Any() ? new ObservableCollection<DayOfTheWeekSwitch>(existingEvent.RecurrancePattern.DaysOfTheWeek.ConvertAll(x => new DayOfTheWeekSwitch() { Day = x, IsChecked = true }).ToList()) : new ObservableCollection<DayOfTheWeekSwitch>();
                    foreach (var r in selectedDays)
                    {
                        RecurrenceDays.First(x => x.Day == r.Day).IsChecked = true;
                    }
                    switch (existingEvent.RecurrancePattern.Frequency)
                    {
                        case RecurrenceFrequency.MonthlyOnDay:
                        case RecurrenceFrequency.YearlyOnDay:
                            IsMonthDaySpecific = false;
                            SelectedRecurrenceMonthWeek = existingEvent.RecurrancePattern.DayIterationOffSetPosition.ToString();
                            SelectedMonthWeekRecurrenceDay = existingEvent.RecurrancePattern.DaysOfTheWeek?.First().ToString();
                            break;
                        case RecurrenceFrequency.Monthly:
                        case RecurrenceFrequency.Yearly:
                            SelectedRecurrenceMonthDay = existingEvent.RecurrancePattern.DayOfTheMonth;
                            break;
                    }
                    SelectedRecurrenceYearlyMonth = existingEvent.RecurrancePattern.MonthOfTheYear.ToString();
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
            && (!CanAlterRecurrence || StartDate < RecurrenceEndDate || SelectedRecurrenceEndType == RecurrenceEndType.Indefinitely || (RecurrenceEndInterval.HasValue && RecurrenceEndInterval.Value > 0));

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
            set => SetProperty(ref url, value);
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
        public bool CanAlterRecurrence => SelectedRecurrenceType != RecurrenceFrequency.None.ToString();

        public bool IsDaily => SelectedRecurrenceType == RecurrenceFrequency.Daily.ToString();

        public bool IsWeekly => SelectedRecurrenceType == RecurrenceFrequency.Weekly.ToString();

        public bool IsMonthlyOrYearly => SelectedRecurrenceType == RecurrenceFrequency.MonthlyOnDay.ToString() || SelectedRecurrenceType == RecurrenceFrequency.Monthly.ToString() || SelectedRecurrenceType == RecurrenceFrequency.YearlyOnDay.ToString() || SelectedRecurrenceType == RecurrenceFrequency.Yearly.ToString();

        public bool IsYearly => SelectedRecurrenceType == RecurrenceFrequency.Yearly.ToString() || SelectedRecurrenceType == RecurrenceFrequency.YearlyOnDay.ToString();

        bool isEveryWeekday;

        public bool IsEveryWeekday
        {
            get => isEveryWeekday;
            set
            {
                if (SetProperty(ref isEveryWeekday, value))
                {
                    if (value)
                    {
                        RecurrenceInterval = 0;
                        RecurrenceDays = recurrenceWeekdays;
                    }
                    else
                    {
                        RecurrenceInterval = 1;
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
                        SelectedRecurrenceMonthWeek = string.Empty;
                        SelectedMonthWeekRecurrenceDay = string.Empty;
                    }
                    else
                    {
                        SelectedRecurrenceMonthDay = 0;
                        SelectedRecurrenceMonthWeek = "First";
                        SelectedMonthWeekRecurrenceDay = DayOfTheWeek.Monday.ToString();
                    }
                }
            }
        }

        public ObservableCollection<DayOfTheWeekSwitch> RecurrenceDays { get; set; } = new ObservableCollection<DayOfTheWeekSwitch>()
        {
            new DayOfTheWeekSwitch() { Day = DayOfTheWeek.Monday },
            new DayOfTheWeekSwitch() { Day = DayOfTheWeek.Tuesday },
            new DayOfTheWeekSwitch() { Day = DayOfTheWeek.Wednesday },
            new DayOfTheWeekSwitch() { Day = DayOfTheWeek.Thursday },
            new DayOfTheWeekSwitch() { Day = DayOfTheWeek.Friday },
            new DayOfTheWeekSwitch() { Day = DayOfTheWeek.Saturday },
            new DayOfTheWeekSwitch() { Day = DayOfTheWeek.Sunday }
        };

        public ObservableCollection<string> RecurrenceTypes { get; } = new ObservableCollection<string>()
        {
            RecurrenceFrequency.None.ToString(),
            RecurrenceFrequency.Daily.ToString(),
            RecurrenceFrequency.Weekly.ToString(),
            RecurrenceFrequency.Monthly.ToString(),
            RecurrenceFrequency.Yearly.ToString()
        };

        string selectedRecurrenceType = RecurrenceFrequency.None.ToString();

        public string SelectedRecurrenceType
        {
            get => selectedRecurrenceType;

            set
            {
                if (SetProperty(ref selectedRecurrenceType, value) && selectedRecurrenceType != null)
                {
                    OnPropertyChanged(nameof(CanAlterRecurrence));
                    OnPropertyChanged(nameof(DisplayTimeInformation));
                    OnPropertyChanged(nameof(IsDaily));
                    OnPropertyChanged(nameof(IsWeekly));
                    OnPropertyChanged(nameof(IsMonthlyOrYearly));
                    OnPropertyChanged(nameof(IsYearly));
                    OnPropertyChanged(nameof(SelectedRecurrenceTypeDisplay));
                    RecurrenceInterval = 1;
                }
            }
        }

        public string SelectedRecurrenceTypeDisplay => SelectedRecurrenceType.Replace("ily", "yly").Replace("ly", "(s)");

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

        public ObservableCollection<string> RecurrenceMonthWeek { get; } = new ObservableCollection<string>()
        {
            IterationOffset.First.ToString(),
            IterationOffset.Second.ToString(),
            IterationOffset.Third.ToString(),
            IterationOffset.Fourth.ToString(),
            IterationOffset.Last.ToString()
        };

        string selectedRecurrenceMonthWeek = IterationOffset.NotSet.ToString();

        public string SelectedRecurrenceMonthWeek
        {
            get => selectedRecurrenceMonthWeek;
            set => SetProperty(ref selectedRecurrenceMonthWeek, value);
        }

        string selectedMonthWeekRecurrenceDay;

        public string SelectedMonthWeekRecurrenceDay
        {
            get => selectedMonthWeekRecurrenceDay;
            set => SetProperty(ref selectedMonthWeekRecurrenceDay, value);
        }

        public ObservableCollection<string> RecurrenceYearlyMonth { get; } = new ObservableCollection<string>()
        {
            MonthOfTheYear.January.ToString(),
            MonthOfTheYear.February.ToString(),
            MonthOfTheYear.March.ToString(),
            MonthOfTheYear.April.ToString(),
            MonthOfTheYear.May.ToString(),
            MonthOfTheYear.June.ToString(),
            MonthOfTheYear.July.ToString(),
            MonthOfTheYear.August.ToString(),
            MonthOfTheYear.September.ToString(),
            MonthOfTheYear.October.ToString(),
            MonthOfTheYear.November.ToString(),
            MonthOfTheYear.December.ToString()
        };

        string selectedRecurrenceYearlyMonth = MonthOfTheYear.January.ToString();

        public string SelectedRecurrenceYearlyMonth
        {
            get => selectedRecurrenceYearlyMonth;
            set
            {
                if (SetProperty(ref selectedRecurrenceYearlyMonth, value) && value != MonthOfTheYear.NotSet.ToString())
                {
                    var days = Enumerable.Range(1, System.DateTime.DaysInMonth(StartDate.Year, (int)(MonthOfTheYear)Enum.Parse(typeof(MonthOfTheYear), value))).Select(x => (uint)x).ToList();
                    RecurrenceMonthDay.Clear();
                    days.ForEach(x => RecurrenceMonthDay.Add(x));
                    SelectedRecurrenceMonthDay = 1;
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
            var newEvent = new DeviceEvent()
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
                List<DayOfTheWeek> daysOfTheWeek = null;
                var selectedFrequency = (RecurrenceFrequency)Enum.Parse(typeof(RecurrenceFrequency), SelectedRecurrenceType);
                var dayIterationOffset = IterationOffset.NotSet;
                switch (selectedFrequency)
                {
                    case RecurrenceFrequency.Daily:
                    case RecurrenceFrequency.Weekly:
                        daysOfTheWeek = RecurrenceDays.Where(x => x.IsChecked).ToList().ConvertAll(x => x.Day);
                        break;
                    default:
                        if (!string.IsNullOrWhiteSpace(SelectedMonthWeekRecurrenceDay))
                        {
                            daysOfTheWeek = new List<DayOfTheWeek>()
                            {
                                (DayOfTheWeek)Enum.Parse(typeof(DayOfTheWeek), SelectedMonthWeekRecurrenceDay)
                            };
                        }

                        if (!string.IsNullOrWhiteSpace(SelectedRecurrenceMonthWeek))
                        {
                            dayIterationOffset = (IterationOffset)Enum.Parse(typeof(IterationOffset), SelectedRecurrenceMonthWeek);
                        }
                        break;
                }

                newEvent.RecurrancePattern = new RecurrenceRule()
                {
                    Frequency = selectedFrequency,
                    Interval = RecurrenceInterval,
                    DaysOfTheWeek = daysOfTheWeek,
                    DayOfTheMonth = SelectedRecurrenceMonthDay,
                    DayIterationOffSetPosition = dayIterationOffset,
                    MonthOfTheYear = (MonthOfTheYear)Enum.Parse(typeof(MonthOfTheYear), SelectedRecurrenceYearlyMonth),
                    EndDate = RecurrenceEndDate,
                    TotalOccurrences = RecurrenceEndInterval
                };
            }

            if (string.IsNullOrEmpty(EventId))
            {
                var eventId = await Calendar.CreateCalendarEvent(newEvent);
                await DisplayAlertAsync("Created event id: " + eventId);
            }
            else
            {
                if (await Calendar.UpdateCalendarEvent(newEvent))
                {
                    await DisplayAlertAsync("Updated event id: " + newEvent.Id);
                }
            }
        }
    }
}
