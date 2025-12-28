using Microsoft.EntityFrameworkCore;
using ZambeelApp.Models;
using System.Data;

namespace ZambeelApp.Services
{
    public class Service_StoredProcs : IZambeelService
    {
        private readonly ZambeelPlusContext _context;

        public Service_StoredProcs(ZambeelPlusContext context)
        {
            _context = context;
        }

        public string RegisterStudent(string email, string name, int schoolId, int deptId, int yearEnroll)
        {
            try
            {
                _context.Database.ExecuteSqlRaw(
                    "EXEC sp_RegisterNewStudent @p0, @p1, @p2, @p3, @p4",
                    email, name, schoolId, deptId, yearEnroll
                );
                return "Success: Student Registered via Stored Procedure.";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public string EnrollStudent(int studentId, int courseId, string semester)
        {
            try
            {
                _context.Database.ExecuteSqlRaw(
                    "EXEC sp_EnrollStudent @p0, @p1, @p2",
                    studentId, courseId, semester
                );
                Console.WriteLine("Success: Student Enrolled via Stored Procedure.");
                return "Success: Student Enrolled via Stored Procedure.";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed: Student Enrollment failed via Stored Procedure.");
                return $"Error: {ex.Message}";
            }
        }

        public List<FinanceReportDTO> GetPendingDefaulters()
        {
            var result = new List<FinanceReportDTO>();

            var conn = _context.Database.GetDbConnection();
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "EXEC sp_Report_FinanceOutstanding";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new FinanceReportDTO
                            {
                                StudentName = reader.GetString(0), // Name is col 0
                                OutstandingAmount = reader.GetDecimal(1) // Amount is col 1
                            });
                        }
                    }
                }
            }
            finally
            {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
            return result;
        }

        public List<CoursesBefore> GetAllCourses()
        {
            return _context.CoursesBefores.ToList();
        }

        public List<FinanceReportDTO> GetStudentFinances(int studentId)
        {
            // Using LINQ for StoredProcs service - aggregate all pending dues
            try
            {
                var studentName = _context.Students
                    .Where(s => s.StudentId == studentId)
                    .Select(s => s.Name)
                    .FirstOrDefault() ?? "Unknown";

                var totalOutstanding = _context.Finances
                    .Where(f => f.StudentId == studentId && f.IsPaid == false)
                    .Sum(f => f.Amount) ?? 0;

                var result = new List<FinanceReportDTO>();
                if (totalOutstanding > 0 || _context.Finances.Any(f => f.StudentId == studentId))
                {
                    result.Add(new FinanceReportDTO
                    {
                        StudentName = $"{studentName} - Total Outstanding",
                        OutstandingAmount = totalOutstanding
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetStudentFinances: {ex.Message}");
                return new List<FinanceReportDTO>();
            }
        }

        public List<ResidencyDTO> GetStudentResidencies(int studentId)
        {
            try
            {
                var result = _context.Residencies
                    .Where(r => r.StudentId == studentId)
                    .Select(r => new ResidencyDTO
                    {
                        HostelName = r.HostelName,
                        RoomNumber = r.RoomNumber,
                        IsPaid = r.IsPaid ?? false
                    })
                    .ToList();

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetStudentResidencies: {ex.Message}");
                return new List<ResidencyDTO>();
            }
        }

        public StudentDTO? ValidateStudentLogin(int studentId, string password)
        {
            try
            {
                var student = _context.Students.FirstOrDefault(s => s.StudentId == studentId);
                if (student == null) return null;

                // Simple password validation (in production we might use hashing)
                if (student.Email != password) return null;

                return new StudentDTO
                {
                    StudentId = student.StudentId,
                    Name = student.Name,
                    Email = student.Email,
                    SchoolId = student.SchoolId,
                    DepartmentId = student.DepartmentId
                };
            }
            catch
            {
                return null;
            }
        }

        public StudentInfoDTO? GetStudentInfo(int studentId)
        {
            try
            {
                var student = _context.Students
                    .Include(s => s.Department)
                    .Include(s => s.School)
                    .FirstOrDefault(s => s.StudentId == studentId);

                if (student == null) return null;

                var schoolId = student.SchoolId ?? 0;
                var studentsInSchool = _context.Students.Where(s => s.SchoolId == schoolId).Count();
                var professorsInSchool = _context.Professors
                    .Where(p => p.Department!.SchoolId == schoolId)
                    .Distinct()
                    .Count();

                return new StudentInfoDTO
                {
                    StudentId = student.StudentId,
                    StudentName = student.Name,
                    DepartmentName = student.Department?.DepartmentName,
                    SchoolName = student.School?.Name,
                    SchoolId = student.SchoolId ?? 0,
                    StudentsInSchool = studentsInSchool,
                    ProfessorsInSchool = professorsInSchool,
                    Cgpa = student.Gpa,
                    CurrentYear = student.CurrentYear
                };
            }
            catch
            {
                return null;
            }
        }

        public SchoolInfoDTO? GetSchoolInfo(int schoolId)
        {
            try
            {
                var school = _context.Schools.FirstOrDefault(s => s.SchoolId == schoolId);
                if (school == null) return null;

                var students = _context.Students
                    .Where(s => s.SchoolId == schoolId)
                    .Select(s => new StudentBasicDTO
                    {
                        StudentId = s.StudentId,
                        Name = s.Name,
                        Email = s.Email
                    })
                    .ToList();

                var professors = _context.Professors
                    .Where(p => p.Department!.SchoolId == schoolId)
                    .Select(p => new ProfessorDTO
                    {
                        ProfessorId = p.ProfessorId,
                        Name = p.Name,
                        Email = p.Email
                    })
                    .ToList();

                return new SchoolInfoDTO
                {
                    SchoolId = school.SchoolId,
                    SchoolName = school.Name,
                    Students = students,
                    Professors = professors
                };
            }
            catch
            {
                return null;
            }
        }

        public List<FreeRoomDTO> GetFreeRooms()
        {
            try
            {
                var freeRooms = new List<FreeRoomDTO>();
                return freeRooms;
            }
            catch
            {
                return new List<FreeRoomDTO>();
            }
        }

        public List<WardenRosterDTO> GetWardenRoster()
        {
            try
            {
                // Get all residencies
                var allResidencies = _context.Residencies.Include(r => r.Student).ToList();
                System.Diagnostics.Debug.WriteLine($"Total Residencies in DB: {allResidencies.Count}");

                // Filter for those with room numbers
                var roster = allResidencies
                    .Where(r => r.RoomNumber != null && r.RoomNumber.Trim() != "")
                    .Select(r => new WardenRosterDTO
                    {
                        StudentId = r.StudentId,
                        StudentName = r.Student?.Name ?? "Unknown",
                        RoomNumber = r.RoomNumber,
                        HostelName = r.HostelName
                    })
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Warden Roster after filtering: {roster.Count}");
                foreach (var entry in roster)
                {
                    System.Diagnostics.Debug.WriteLine($"  - Student {entry.StudentId} ({entry.StudentName}) in {entry.HostelName}/{entry.RoomNumber}");
                }

                return roster;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetWardenRoster: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                return new List<WardenRosterDTO>();
            }
        }

        public List<string> GetStudentSemesters(int studentId)
        {
            try
            {
                var semesters = _context.Enrollments
                    .Where(e => e.StudentId == studentId)
                    .Select(e => e.Semester)
                    .Distinct()
                    .Where(s => s != null)
                    .Select(s => s!)
                    .ToList();

                // Always include Spring 2026 as an available semester
                if (!semesters.Contains("Spring 2026"))
                {
                    semesters.Add("Spring 2026");
                }

                // Sort with Spring 2026 first, then descending
                return semesters.OrderByDescending(s => s).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetStudentSemesters: {ex.Message}");
                return new List<string> { "Spring 2026" };
            }
        }

        public List<SemesterCourseDTO> GetSemesterCourses(int studentId, string semester)
        {
            try
            {
                // Get enrollments for the semester
                var enrollments = _context.Enrollments
                    .Include(e => e.Course)
                    .Where(e => e.StudentId == studentId && e.Semester == semester)
                    .ToList();

                // Get grades from Gradebook for this student
                var gradebook = _context.Gradebooks
                    .Where(g => g.StudentId == studentId)
                    .ToDictionary(g => g.CourseId, g => g.LetterGrade);

                var courses = enrollments.Select(e => new SemesterCourseDTO
                {
                    CourseId = e.CourseId,
                    CourseName = e.Course.Name,
                    CreditHours = e.Credits,
                    Grade = gradebook.ContainsKey(e.CourseId) ? gradebook[e.CourseId] : e.Grade,
                    Status = e.Status
                }).ToList();

                return courses;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetSemesterCourses: {ex.Message}");
                return new List<SemesterCourseDTO>();
            }
        }

        public List<SchoolBasicDTO> GetAllSchools()
        {
            try
            {
                return _context.Schools
                    .Select(s => new SchoolBasicDTO
                    {
                        SchoolId = s.SchoolId,
                        Name = s.Name
                    })
                    .OrderBy(s => s.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetAllSchools: {ex.Message}");
                return new List<SchoolBasicDTO>();
            }
        }

        public List<AvailableCourseDTO> GetAvailableCoursesForStudent(int studentId, int schoolId)
        {
            try
            {
                // Get courses the student has completed (passed) - check Gradebook LetterGrade
                var completedCourseIds = _context.Gradebooks
                    .Where(g => g.StudentId == studentId &&
                           g.LetterGrade != null &&
                           g.LetterGrade != "F")
                    .Select(g => g.CourseId)
                    .ToHashSet();

                // Get courses the student is already enrolled in
                var enrolledCourseIds = _context.Enrollments
                    .Where(e => e.StudentId == studentId)
                    .Select(e => e.CourseId)
                    .ToHashSet();

                // Get courses from the selected school
                var schoolCourses = _context.CoursesBefores
                    .Include(c => c.Department)
                    .Include(c => c.Professor)
                    .Include(c => c.PreReq)
                    .Where(c => c.Department.SchoolId == schoolId)
                    .ToList();

                var result = schoolCourses
                    .Where(c => !enrolledCourseIds.Contains(c.CourseId)) // Not already enrolled
                    .Select(c => new AvailableCourseDTO
                    {
                        CourseId = c.CourseId,
                        CourseName = c.Name,
                        CreditHours = c.CreditHours,
                        ProfessorName = c.Professor?.Name ?? "TBA",
                        Timings = c.Timings ?? "TBA",
                        DepartmentName = c.Department?.DepartmentName,
                        PreReqMet = c.PreReqId == null || completedCourseIds.Contains(c.PreReqId.Value),
                        PreReqCourseName = c.PreReq?.Name
                    })
                    .OrderBy(c => c.DepartmentName)
                    .ThenBy(c => c.CourseName)
                    .ToList();

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetAvailableCoursesForStudent: {ex.Message}");
                return new List<AvailableCourseDTO>();
            }
        }

        public CourseDetailDTO? GetCourseDetails(int studentId, int courseId, string semester)
        {
            try
            {
                // Get course info
                var course = _context.CoursesBefores.FirstOrDefault(c => c.CourseId == courseId);
                if (course == null) return null;

                var result = new CourseDetailDTO
                {
                    CourseId = courseId,
                    CourseName = course.Name,
                    Semester = semester
                };

                // Get gradebook for this student and course
                var gradebook = _context.Gradebooks
                    .FirstOrDefault(g => g.StudentId == studentId && g.CourseId == courseId);
                
                if (gradebook != null)
                {
                    result.Gradebook = new GradebookDetailDTO
                    {
                        GradebookId = gradebook.GradebookId,
                        ExamMarks = gradebook.ExamMarks,
                        QuizMarks = gradebook.QuizMarks,
                        AssignmentMarks = gradebook.AssignmentMarks,
                        AttendancePercent = gradebook.AttendancePercent,
                        ProjectMarks = gradebook.ProjectMarks,
                        LetterGrade = gradebook.LetterGrade
                    };

                    // Get assignments for this gradebook
                    result.Assignments = _context.Assignments
                        .Where(a => a.GradebookId == gradebook.GradebookId && a.StudentId == studentId)
                        .Select(a => new AssignmentDTO
                        {
                            AssignmentId = a.AssignmentId,
                            Name = a.Name,
                            OpenDate = a.OpenDate,
                            DueDate = a.DueDate,
                            Marks = a.Marks,
                            ManualLink = a.ManualLink,
                            SubmissionLink = a.SubmissionLink
                        })
                        .ToList();
                }

                // Get final schedule for this student and course
                var finalSchedule = _context.FinalSchedules
                    .FirstOrDefault(f => f.StudentId == studentId && f.CourseId == courseId);
                
                if (finalSchedule != null)
                {
                    result.FinalSchedule = new FinalScheduleDTO
                    {
                        ScheduleId = finalSchedule.ScheduleId,
                        ExamDate = finalSchedule.ExamDate,
                        ExamTime = finalSchedule.ExamTime
                    };
                }

                // Get roster from Courses_after table (case-insensitive match)
                var courseAfter = _context.CoursesAfters
                    .FirstOrDefault(ca => ca.CourseId == courseId && 
                        ca.Semester.ToLower() == semester.ToLower());
                
                if (courseAfter != null)
                {
                    result.Roster = new CourseRosterDTO
                    {
                        NumStudents = courseAfter.NumStudents ?? 0,
                        RosterList = courseAfter.Roster
                    };

                    // Get resources for this course section
                    result.Resources = _context.ResourcesInstructors
                        .Where(r => r.CourseAid == courseAfter.CourseAid)
                        .Select(r => new ResourceDTO
                        {
                            ResourceId = r.ResourceId,
                            Lectures = r.Lectures,
                            Tutorials = r.Tutorials,
                            PracticeProblems = r.PracticeProblems,
                            UploadDate = r.UploadDate
                        })
                        .ToList();
                }
                else
                {
                    // Fallback: Build roster from Enrollment table if CoursesAfter data not available
                    var enrolledStudents = _context.Enrollments
                        .Where(e => e.CourseId == courseId && 
                            e.Semester != null && e.Semester.ToLower() == semester.ToLower())
                        .Join(_context.Students,
                            e => e.StudentId,
                            s => s.StudentId,
                            (e, s) => new { s.StudentId, s.Name })
                        .ToList();

                    if (enrolledStudents.Any())
                    {
                        result.Roster = new CourseRosterDTO
                        {
                            NumStudents = enrolledStudents.Count,
                            RosterList = string.Join(", ", enrolledStudents.Select(s => s.Name))
                        };
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetCourseDetails: {ex.Message}");
                return null;
            }
        }

        public Dictionary<int, List<TimetableSlotDTO>> GetCourseTimetables(List<int> courseIds)
        {
            try
            {
                var result = new Dictionary<int, List<TimetableSlotDTO>>();
                
                var timetables = _context.Timetables
                    .Where(t => courseIds.Contains(t.CourseId ?? 0))
                    .ToList();
                
                foreach (var tt in timetables)
                {
                    if (tt.CourseId.HasValue)
                    {
                        if (!result.ContainsKey(tt.CourseId.Value))
                        {
                            result[tt.CourseId.Value] = new List<TimetableSlotDTO>();
                        }
                        
                        result[tt.CourseId.Value].Add(new TimetableSlotDTO
                        {
                            Day = tt.Day,
                            SlotStart = tt.SlotStart,
                            SlotEnd = tt.SlotEnd
                        });
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetCourseTimetables: {ex.Message}");
                return new Dictionary<int, List<TimetableSlotDTO>>();
            }
        }
    }
}
