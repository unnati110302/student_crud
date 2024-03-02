using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using student_crud.Models;


namespace student_crud.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        private readonly StudentContext _studentContext;
        public StudentController(StudentContext studentContext)
        {
            _studentContext = studentContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudentDAO>>> GetStudents(int pageNumber = 1, int pageSize = 5, string search = "", string sortOrder = "none")
        {
            string sql = "SELECT a.ID, a.Code, a.Name, a.Email, a.Mobile, a.Address1, a.Address2, a.State, a.City, a.Gender, a.Status, " +
                "a.IsActive, a.CreatedBy, a.CreatedOn, a.ModifiedBy, a.ModifiedOn, s.Name as StateName, c.Name as CityName " +
                "FROM Students a " +
                "LEFT OUTER JOIN States s ON a.State = s.SId " +
                "LEFT OUTER JOIN Cities c ON a.City = c.CId " +
                "WHERE a.IsActive = 1";

            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += $" AND (a.Name LIKE '%{search}%' OR a.Email LIKE '%{search}%')";
            }

            if (sortOrder.ToLower() != "none") 
            {
                sql += $" ORDER BY a.Name {sortOrder}";
            }

            var activeStudents = await _studentContext.StudentDAOs.FromSqlRaw(sql).ToListAsync();
            
            if (activeStudents == null || !activeStudents.Any())
            {
                return NotFound();
            }

            var totalCount = activeStudents.Count();

            var paginatedStudents = await PaginatedList<StudentDAO>.Create(activeStudents, pageNumber, pageSize);

            var totalPages = paginatedStudents.TotalPages;

            return Ok(new { Data = paginatedStudents, TotalPages = totalPages });

        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Student>> GetStudent(int id)
        {
            if (_studentContext.Students == null)
            {
                return NotFound();
            }
            var student = await _studentContext.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            return student;
        }

        [HttpPost]
        public async Task<ActionResult<Student>> PostStudent(Student student)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (await IsDuplicateEmailOrMobile(student.Email, student.Mobile))
            {
                return BadRequest(new { ErrorMessage = "A record with the same email or mobile number already exists.", DuplicateFound = true });
            }
            int maxId = await _studentContext.Students.MaxAsync(s => (int?)s.ID) ?? 0;
            int newId = maxId + 1;
            string studentCode = $"{DateTime.Now:yyyyMMdd}00{newId}";
            student.Code = studentCode;

            _studentContext.Students.Add(student);
            await _studentContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStudent), new { id = student.ID }, student);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> PutStudent(int id, Student student)
        {
            if (id != student.ID)
            {
                return BadRequest();
            }

            _studentContext.Entry(student).State = EntityState.Modified;

            try
            {
                await _studentContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteStudent(int id)
        {
            if (_studentContext.Students == null)
            {
                return NotFound();
            }

            var student = await _studentContext.Students.FindAsync(id);

            if (student == null)
            {
                return NotFound();
            }

            student.IsActive = 0;
            await _studentContext.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("delete-multiple")]
        public async Task<ActionResult> DeleteStudents([FromBody] List<int> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return BadRequest("No IDs provided for deletion.");
            }

            var students = await _studentContext.Students
                                                .Where(s => ids.Contains(s.ID))
                                                .ToListAsync();

            if (students == null || students.Count == 0)
            {
                return NotFound("No matching students found for deletion.");
            }

            foreach (var student in students)
            {
                student.IsActive = 0;
                // _studentContext.Students.Remove(student);
            }

            await _studentContext.SaveChangesAsync();

            return Ok("Selected students have been deleted.");
        }

        private async Task<bool> IsDuplicateEmailOrMobile(string email, string mobile)
        {
            return await _studentContext.Students.AnyAsync(s => s.Email == email || s.Mobile == mobile);
        }
    }
}