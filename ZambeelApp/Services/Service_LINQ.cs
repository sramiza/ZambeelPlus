using Microsoft.EntityFrameworkCore;
using ZambeelApp.Models;

namespace ZambeelApp.Services
{
    public class Service_LINQ : IZambeelService
    {
        private readonly ZambeelPlusContext _context;

        public Service_LINQ(ZambeelPlusContext context)
        {
            _context = context;
        }

        public string RegisterStudent(string email, string name, int schoolId, int deptId, int yearEnroll)
        {
            try
            {
                // 1. Calculate Graduation Year
                int gradYear = yearEnroll + 4;

                // 2. Generate ID Logic (Simplified C# version of your SQL logic)
                string yearPrefix = gradYear.ToString().Substring(2, 2);
                string schoolPrefix = schoolId.ToString("D2");
                string prefix = yearPrefix + schoolPrefix;

                // Find max existing ID with this prefix
                int minId = int.Parse(prefix + "0000");
                int maxIdRange = int.Parse(prefix + "9999");

                var maxExisting = _context.Students
                    .Where(s => s.StudentId >= minId && s.StudentId <= maxIdRange)
                    .Max(s => (int?)s.StudentId);

                int nextSeq = 1;
                if (maxExisting.HasValue)
                {
                    nextSeq = int.Parse(maxExisting.Value.ToString().Substring(4)) + 1;
                }

                int newId = int.Parse(prefix + nextSeq.ToString("D4"));

                // 3. Insert
                var student = new Student
                {
                    StudentId = newId,
                    Email = email,
                    Name = name,
                    YearOfEnrollment = yearEnroll,
                    YearOfGraduation = gradYear,
                    SchoolId = schoolId,
                    DepartmentId = deptId,
                    FinanceStatus = true
                };

                _context.Students.Add(student);
                _context.SaveChanges();

                return $"Success (LINQ): Registered {name} with ID {newId}";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed: Student Enrollment failed via LINQ."); // This would print to the terminal                
                return $"Error: {ex.Message}";
            }
        }

        public string EnrollStudent(int studentId, int courseId, string semester)
        {
            try
            {
                // 1. Get Course Info
                var course = _context.CoursesBefores.Find(courseId);
                if (course == null) return "Course not found";
        
                // 2. CHECK: Limit to 20 Credit Hours
                // We calculate the sum of credits for this student in this specific semester
                int currentCredits = _context.Enrollments
                    .Where(e => e.StudentId == studentId && e.Semester == semester)
                    .Sum(e => e.Credits) ?? 0;
        
                // Check if adding the new course exceeds the limit
                if (currentCredits + course.CreditHours > 20)
                {
                    return $"Error: Credit limit is 20. You have {currentCredits}, trying to add {course.CreditHours}.";
                }
        
                // 3. Create Enrollment
                var enroll = new Enrollment
                {
                    StudentId = studentId,
                    CourseId = courseId,
                    Credits = course.CreditHours,
                    Status = "Enrolled",
                    Semester = semester
                };
        
                _context.Enrollments.Add(enroll);
                _context.SaveChanges();
        
                Console.WriteLine("Success (LINQ): Enrolled successfully.");
                return "Success (LINQ): Enrolled successfully.";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public List<FinanceReportDTO> GetPendingDefaulters()
        {
            // LINQ Logic: Join Finances and Student, filter by IsPaid = false
            var query = from f in _context.Finances
                        join s in _context.Students on f.StudentId equals s.StudentId
                        where f.IsPaid == false
                        group f by new { s.StudentId, s.Name } into g
                        select new FinanceReportDTO
                        {
                            StudentName = g.Key.Name,
                            OutstandingAmount = g.Sum(x => x.Amount) ?? 0
                        };

            return query.OrderByDescending(x => x.OutstandingAmount).ToList();
        }

        public List<CoursesBefore> GetAllCourses()
        {
            return _context.CoursesBefores.ToList();
        }

        public List<FinanceReportDTO> GetStudentFinances(int studentId)
        {
            // LINQ Logic: Get all finance records for a student and aggregate
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

        public List<ResidencyDTO> GetStudentResidencies(int studentId)
        {
            // LINQ Logic: Get all residency records for a student
            var query = from r in _context.Residencies
                        where r.StudentId == studentId
                        select new ResidencyDTO
                        {
                            HostelName = r.HostelName,
                            RoomNumber = r.RoomNumber,
                            IsPaid = r.IsPaid ?? false
                        };

            return query.ToList();
        }

        public StudentDTO? ValidateStudentLogin(int studentId, string password)
        {
            try
            {
                // For now, using password = email as simple validation
                var student = _context.Students.FirstOrDefault(s => s.StudentId == studentId);
                if (student == null) return null;
                
                // Simple password validation (in production we could use hashing)
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
                // Get all rooms with residencies, then find unallocated ones
                var allocatedRooms = _context.Residencies
                    .Where(r => !string.IsNullOrEmpty(r.RoomNumber))
                    .Select(r => r.RoomNumber)
                    .Distinct()
                    .ToList();

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
