using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Samples.ViewModel
{
    public class TestDumbReference
    {
        public DayOfTheWeek Day { get; set; }

        public override string ToString() => Day.ToString();
    }

    public class CalendarEventAddViewModel : BaseViewModel
    {
        static readonly ObservableCollection<TestDumbReference> RecurrenceWeekdays = new ObservableCollection<TestDumbReference>()
        {
            new TestDumbReference() { Day = DayOfTheWeek.Monday },
            new TestDumbReference() { Day = DayOfTheWeek.Tuesday },
            new TestDumbReference() { Day = DayOfTheWeek.Wednesday },
            new TestDumbReference() { Day = DayOfTheWeek.Thursday },
            new TestDumbReference() { Day = DayOfTheWeek.Friday }
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
                AllDay = existingEvent.AllDay;
                StartDate = existingEvent.StartDate.Date;
                EndDate = existingEvent.EndDate?.Date ?? existingEvent.StartDate.Date;
                StartTime = existingEvent.StartDate.TimeOfDay;
                EndTime = existingEvent.EndDate?.TimeOfDay ?? existingEvent.StartDate.TimeOfDay;
                if (existingEvent.RecurrancePattern != null)
                {
                    SelectedRecurrenceType = existingEvent.RecurrancePattern.Frequency.ToString();
                    SelectedRecurrenceInterval = existingEvent.RecurrancePattern.Interval;
                    SelectedRecurrenceDays = existingEvent.RecurrancePattern.DaysOfTheWeek != null ? new ObservableCollection<TestDumbReference>(existingEvent.RecurrancePattern.DaysOfTheWeek.ConvertAll(x => new TestDumbReference() { Day = x }).ToList()) : null;
                    if (existingEvent.RecurrancePattern.Frequency == RecurrenceFrequency.MonthlyOnDay || existingEvent.RecurrancePattern.Frequency == RecurrenceFrequency.YearlyOnDay)
                    {
                        IsMonthDaySpecific = false;
                        SelectedRecurrenceMonthWeek = existingEvent.RecurrancePattern.DayIterationOffSetPosition != null && existingEvent.RecurrancePattern.DayIterationOffSetPosition.Count > 0 ? existingEvent.RecurrancePattern.DayIterationOffSetPosition.First().ToString() : IterationOffset.NotSet.ToString();
                        SelectedMonthWeekRecurrenceDay = existingEvent.RecurrancePattern.DaysOfTheWeek.First().ToString();
                    }
                    else if (existingEvent.RecurrancePattern.Frequency == RecurrenceFrequency.Monthly || existingEvent.RecurrancePattern.Frequency == RecurrenceFrequency.Yearly)
                    {
                        SelectedRecurrenceMonthDay = existingEvent.RecurrancePattern.DaysOfTheMonth?.First() ?? 0;
                    }
                    SelectedRecurrenceYearlyMonth = existingEvent.RecurrancePattern.MonthsOfTheYear != null && existingEvent.RecurrancePattern.MonthsOfTheYear.Count > 0 ? existingEvent.RecurrancePattern.MonthsOfTheYear.First().ToString() : MonthOfTheYear.January.ToString();
                    if (existingEvent.RecurrancePattern.EndDate.HasValue)
                    {
                        RecurrenceEndDate = existingEvent.RecurrancePattern.EndDate.Value.DateTime;
                    }
                    else
                    {
                        RecurrenceEndDate = null;
                        RecursIndefinitely = true;
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

        public bool CanCreateOrUpdateEvent => !string.IsNullOrWhiteSpace(EventTitle) && !string.IsNullOrWhiteSpace(Description) && !string.IsNullOrWhiteSpace(EventLocation)
            && ((EndDate.Date == StartDate.Date && (EndTime > StartTime || AllDay)) || EndDate.Date > StartDate.Date)
            && (!CanAlterRecurrence || StartDate < RecurrenceEndDate || RecursIndefinitely);

        string description;

        public string Description
        {
            get => description;
            set
            {
                if (SetProperty(ref description, value))
                {
                    OnPropertyChanged(nameof(CanCreateOrUpdateEvent));
                }
            }
        }

        string eventLocation;

        public string EventLocation
        {
            get => eventLocation;
            set
            {
                if (SetProperty(ref eventLocation, value))
                {
                    OnPropertyChanged(nameof(CanCreateOrUpdateEvent));
                }
            }
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
                        SelectedRecurrenceInterval = 0;
                        SelectedRecurrenceDays = RecurrenceWeekdays;
                    }
                    else
                    {
                        SelectedRecurrenceInterval = 1;
                        SelectedRecurrenceDays.Clear();
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

        bool recursIndefinitely = false;

        public bool RecursIndefinitely
        {
            get => recursIndefinitely;
            set
            {
                if (SetProperty(ref recursIndefinitely, value))
                {
                    if (value)
                    {
                        RecurrenceEndDate = null;
                    }
                    else
                    {
                        RecurrenceEndDate = DateTime.Now.AddMonths(6);
                    }
                }
            }
        }

        public ObservableCollection<TestDumbReference> RecurrenceDays { get; } = new ObservableCollection<TestDumbReference>()
        {
            new TestDumbReference() { Day = DayOfTheWeek.Monday },
            new TestDumbReference() { Day = DayOfTheWeek.Tuesday },
            new TestDumbReference() { Day = DayOfTheWeek.Wednesday },
            new TestDumbReference() { Day = DayOfTheWeek.Thursday },
            new TestDumbReference() { Day = DayOfTheWeek.Friday },
            new TestDumbReference() { Day = DayOfTheWeek.Saturday },
            new TestDumbReference() { Day = DayOfTheWeek.Sunday }
        };

        ObservableCollection<TestDumbReference> selectedRecurrenceDays;

        public ObservableCollection<TestDumbReference> SelectedRecurrenceDays
        {
            get => selectedRecurrenceDays;
            set
            {
                // var listCheck = new ObservableCollection<TestDumbReference>();
                // foreach (var v in value)
                // {
                //    if (RecurrenceDays.Contains(v))
                //    {
                //        listCheck.Add(RecurrenceDays.Single(x => x == v));
                //    }
                // }

                SetProperty(ref selectedRecurrenceDays, value);
            }
        }

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
                    OnPropertyChanged(nameof(IsDaily));
                    OnPropertyChanged(nameof(IsWeekly));
                    OnPropertyChanged(nameof(IsMonthlyOrYearly));
                    OnPropertyChanged(nameof(IsYearly));
                    OnPropertyChanged(nameof(SelectedRecurrenceTypeDisplay));
                    SelectedRecurrenceInterval = 1;
                    SelectedRecurrenceDays?.Clear();
                }
            }
        }

        public string SelectedRecurrenceTypeDisplay => SelectedRecurrenceType.Replace("ily", "yly").Replace("ly", "(s)");

        public ObservableCollection<uint> RecurrenceInterval { get; } = new ObservableCollection<uint>()
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10
        };

        uint selectedRecurrenceInterval;

        public uint SelectedRecurrenceInterval
        {
            get => selectedRecurrenceInterval;
            set
            {
                if (SetProperty(ref selectedRecurrenceInterval, value))
                {
                    if (value != 0)
                    {
                        IsEveryWeekday = false;
                    }
                }
            }
        }

        public ObservableCollection<int> RecurrenceMonthDay { get; } = new ObservableCollection<int>()
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31
        };

        int selectedRecurrenceMonthDay = 1;

        public int SelectedRecurrenceMonthDay
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

        string selectedRecurrenceMonthWeek;

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
            set => SetProperty(ref selectedRecurrenceYearlyMonth, value);
        }

        DateTime? recurrenceEndDate = DateTime.Now.Date.AddMonths(6);

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
                StartDate = startDto,
                EndDate = !AllDay ? (DateTimeOffset?)endDto : null
            };

            if (CanAlterRecurrence)
            {
                newEvent.RecurrancePattern = new RecurrenceRule()
                {
                    Frequency = (RecurrenceFrequency)Enum.Parse(typeof(RecurrenceFrequency), SelectedRecurrenceType),
                    Interval = SelectedRecurrenceInterval,
                    DaysOfTheWeek = SelectedRecurrenceDays != null && SelectedRecurrenceDays.Count > 0 ? SelectedRecurrenceDays.ToList().ConvertAll(x => x.Day) : !string.IsNullOrWhiteSpace(SelectedMonthWeekRecurrenceDay) ? new List<DayOfTheWeek>() { (DayOfTheWeek)Enum.Parse(typeof(DayOfTheWeek), SelectedMonthWeekRecurrenceDay) } : null,
                    DaysOfTheMonth = SelectedRecurrenceMonthDay != 0 ? new List<int>() { SelectedRecurrenceMonthDay } : null,
                    DayIterationOffSetPosition = !string.IsNullOrEmpty(SelectedRecurrenceMonthWeek) ? new List<IterationOffset>() { (IterationOffset)Enum.Parse(typeof(IterationOffset), SelectedRecurrenceMonthWeek) } : null,
                    MonthsOfTheYear = !string.IsNullOrEmpty(SelectedRecurrenceYearlyMonth) ? new List<MonthOfTheYear>() { (MonthOfTheYear)Enum.Parse(typeof(MonthOfTheYear), SelectedRecurrenceYearlyMonth) } : null,
                    EndDate = RecurrenceEndDate
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
