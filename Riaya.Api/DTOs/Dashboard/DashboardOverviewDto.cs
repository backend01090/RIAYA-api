namespace Riaya.Api.DTOs.Dashboard;

public class DashboardOverviewDto
{
    public int TotalDoctors { get; set; }
    public int TotalPatients { get; set; }
    public int TotalSpecializations { get; set; }
    public int TotalAppointments { get; set; }
    public int PendingAppointments { get; set; }
    public int ConfirmedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int TodayAppointments { get; set; }
    public int TodayConfirmedAppointments { get; set; }
    public int UpcomingAppointments { get; set; }
    public int TodayVisits { get; set; }
    public int TotalVisits { get; set; }
    public int TotalPrescriptions { get; set; }
    public int TodayPrescriptions { get; set; }

    public List<DashboardRecentAppointmentDto> RecentAppointments { get; set; } = new();
    public List<DashboardRecentVisitDto> RecentVisits { get; set; } = new();
    public List<DashboardRecentPrescriptionDto> RecentPrescriptions { get; set; } = new();
}
