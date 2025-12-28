using System.Data;
using ZambeelApp.Models; // Maps to your scaffolded classes

namespace ZambeelApp.Services
{
    // The Contract: Both BLLs (LINQ and Stored Proc) must implement this.
    public interface IZambeelService
    {
        // 1. Register Student
        string RegisterStudent(string email, string name, int schoolId, int deptId, int yearEnroll);

        // 2. Enroll Student
        string EnrollStudent(int studentId, int courseId, string semester);

        // 3. Get Pending Fees (Reporting) - We use a simple list of objects/DTOs here
        List<FinanceReportDTO> GetPendingDefaulters();

        // Helper to populate dropdowns
        List<CoursesBefore> GetAllCourses();

        // 4. Get Student Finance Info
        List<FinanceReportDTO> GetStudentFinances(int studentId);

        // 5. Get Student Residency Info
        List<ResidencyDTO> GetStudentResidencies(int studentId);

        // 6. Authentication - Validate student login
        StudentDTO? ValidateStudentLogin(int studentId, string password);

        // 7. Get student information (department, school, etc)
        StudentInfoDTO? GetStudentInfo(int studentId);

        // 8. Get school information with students and professors
        SchoolInfoDTO? GetSchoolInfo(int schoolId);

        // 9. Get free rooms in hostels
        List<FreeRoomDTO> GetFreeRooms();

        // 10. Get warden roster (students with room allocations)
        List<WardenRosterDTO> GetWardenRoster();

        // 11. Get student semesters (enrollment history)
        List<string> GetStudentSemesters(int studentId);

        // 12. Get courses with grades for a specific semester
        List<SemesterCourseDTO> GetSemesterCourses(int studentId, string semester);

        // 13. Get all schools
        List<SchoolBasicDTO> GetAllSchools();

        // 14. Get courses by school that student can enroll in (based on prerequisites)
        List<AvailableCourseDTO> GetAvailableCoursesForStudent(int studentId, int schoolId);

        // 15. Get detailed course information for a past semester (assignments, gradebook, final schedule, roster, resources)
        CourseDetailDTO? GetCourseDetails(int studentId, int courseId, string semester);

        // 16. Get timetables for courses
        Dictionary<int, List<TimetableSlotDTO>> GetCourseTimetables(List<int> courseIds);
    }

    // A simple helper class to hold report data
    public class FinanceReportDTO
    {
        public string? StudentName { get; set; }
        public decimal OutstandingAmount { get; set; }
    }

    // A simple helper class to hold residency data
    public class ResidencyDTO
    {
        public string? HostelName { get; set; }
        public string? RoomNumber { get; set; }
        public bool IsPaid { get; set; }
    }

    // Student authentication DTO
    public class StudentDTO
    {
        public int StudentId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public int? SchoolId { get; set; }
        public int? DepartmentId { get; set; }
    }

    // Student information DTO
    public class StudentInfoDTO
    {
        public int StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? DepartmentName { get; set; }
        public string? SchoolName { get; set; }
        public int SchoolId { get; set; }
        public int StudentsInSchool { get; set; }
        public int ProfessorsInSchool { get; set; }
        public decimal? Cgpa { get; set; }
        public string? CurrentYear { get; set; }
    }

    // School information DTO
    public class SchoolInfoDTO
    {
        public int SchoolId { get; set; }
        public string? SchoolName { get; set; }
        public List<StudentBasicDTO> Students { get; set; } = new();
        public List<ProfessorDTO> Professors { get; set; } = new();
    }

    // Student basic info
    public class StudentBasicDTO
    {
        public int StudentId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
    }

    // Professor info
    public class ProfessorDTO
    {
        public int ProfessorId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
    }

    // Free room DTO
    public class FreeRoomDTO
    {
        public string? HostelName { get; set; }
        public string? RoomNumber { get; set; }
        public int? Capacity { get; set; }
    }

    // Warden roster DTO
    public class WardenRosterDTO
    {
        public int StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? RoomNumber { get; set; }
        public string? HostelName { get; set; }
    }

    // Semester course with grade DTO
    public class SemesterCourseDTO
    {
        public int CourseId { get; set; }
        public string? CourseName { get; set; }
        public int? CreditHours { get; set; }
        public string? Grade { get; set; }
        public string? Status { get; set; }
    }

    // School basic info DTO
    public class SchoolBasicDTO
    {
        public int SchoolId { get; set; }
        public string? Name { get; set; }
    }

    // Available course for enrollment DTO
    public class AvailableCourseDTO
    {
        public int CourseId { get; set; }
        public string? CourseName { get; set; }
        public int? CreditHours { get; set; }
        public string? ProfessorName { get; set; }
        public string? Timings { get; set; }
        public string? DepartmentName { get; set; }
        public bool PreReqMet { get; set; }
        public string? PreReqCourseName { get; set; }
    }

    // Course detail DTOs for past semester view
    public class CourseDetailDTO
    {
        public int CourseId { get; set; }
        public string? CourseName { get; set; }
        public string? Semester { get; set; }
        public GradebookDetailDTO? Gradebook { get; set; }
        public List<AssignmentDTO> Assignments { get; set; } = new();
        public FinalScheduleDTO? FinalSchedule { get; set; }
        public CourseRosterDTO? Roster { get; set; }
        public List<ResourceDTO> Resources { get; set; } = new();
    }

    public class GradebookDetailDTO
    {
        public int GradebookId { get; set; }
        public decimal? ExamMarks { get; set; }
        public decimal? QuizMarks { get; set; }
        public decimal? AssignmentMarks { get; set; }
        public decimal? AttendancePercent { get; set; }
        public decimal? ProjectMarks { get; set; }
        public string? LetterGrade { get; set; }
    }

    public class AssignmentDTO
    {
        public int AssignmentId { get; set; }
        public string? Name { get; set; }
        public DateOnly? OpenDate { get; set; }
        public DateOnly? DueDate { get; set; }
        public int? Marks { get; set; }
        public string? ManualLink { get; set; }
        public string? SubmissionLink { get; set; }
    }

    public class FinalScheduleDTO
    {
        public int ScheduleId { get; set; }
        public DateOnly? ExamDate { get; set; }
        public TimeOnly? ExamTime { get; set; }
    }

    public class CourseRosterDTO
    {
        public int NumStudents { get; set; }
        public string? RosterList { get; set; }
    }

    public class ResourceDTO
    {
        public int ResourceId { get; set; }
        public string? Lectures { get; set; }
        public string? Tutorials { get; set; }
        public string? PracticeProblems { get; set; }
        public DateOnly? UploadDate { get; set; }
    }

    public class TimetableSlotDTO
    {
        public string? Day { get; set; }
        public TimeOnly? SlotStart { get; set; }
        public TimeOnly? SlotEnd { get; set; }
    }
}