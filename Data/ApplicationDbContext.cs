using Microsoft.EntityFrameworkCore;
using CollegeManagementSystem.Models;

namespace CollegeManagementSystem.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Inquiry> Inquiries => Set<Inquiry>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Marks> Marks => Set<Marks>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed 7 Science Courses strictly in Alphabetical Order
        modelBuilder.Entity<Course>().HasData(
            new Course
            {
                CourseId = 1,
                CourseCode = "BOT",
                CourseName = "B.Sc. Botany",
                Category = "Life Sciences",
                Description = "Explore plant science, plant diversity, anatomy, physiology, and ecological processes through hands-on laboratory and field observations.",
                Duration = "3 Years (6 Semesters)",
                Fees = "Details will be updated by the institute.",
                Faculty = "Details will be updated by the institute.",
                Certification = "Details will be updated by the institute.",
                PracticalKnowledge = "Herbarium preparation, microscopic examination of plant tissues, physiological experiments, and botanical taxonomy.",
                Eligibility = "Higher Secondary (10+2) with Biology or equivalent recognized examination.",
                ImagePath = "/images/programs/botany/botany.jpg",
                IsActive = true
            },
            new Course
            {
                CourseId = 2,
                CourseCode = "CHM",
                CourseName = "B.Sc. Chemistry",
                Category = "Physical Sciences",
                Description = "Build solid foundations in inorganic, organic, and physical chemistry with regular analytical lab experiments and compound synthesis.",
                Duration = "3 Years (6 Semesters)",
                Fees = "Details will be updated by the institute.",
                Faculty = "Details will be updated by the institute.",
                Certification = "Details will be updated by the institute.",
                PracticalKnowledge = "Volumetric & gravimetric analysis, organic synthesis, qualitative chemical detection, and spectrophotometry.",
                Eligibility = "Higher Secondary (10+2) with Chemistry or equivalent recognized examination.",
                ImagePath = "/images/programs/chemistry/chemistry.jpg",
                IsActive = true
            },
            new Course
            {
                CourseId = 3,
                CourseCode = "CSC",
                CourseName = "B.Sc. Computer Science",
                Category = "Applied & Technological Sciences",
                Description = "Develop computational thinking, programming proficiency, data structures, algorithms, databases, and modern software technologies.",
                Duration = "3 Years (6 Semesters)",
                Fees = "Details will be updated by the institute.",
                Faculty = "Details will be updated by the institute.",
                Certification = "Details will be updated by the institute.",
                PracticalKnowledge = "Structured coding in C/C++/Java/Python, relational database querying, algorithm implementation, and software project development.",
                Eligibility = "Higher Secondary (10+2) with Mathematics/Science or equivalent recognized examination.",
                ImagePath = "/images/programs/computer-science/computer-science.jpg",
                IsActive = true
            },
            new Course
            {
                CourseId = 4,
                CourseCode = "FOR",
                CourseName = "B.Sc. Forensic Science",
                Category = "Applied & Technological Sciences",
                Description = "Apply scientific methods, criminalistics, evidence collection, toxicology, and chemical analysis for objective scientific investigation.",
                Duration = "3 Years (6 Semesters)",
                Fees = "Details will be updated by the institute.",
                Faculty = "Details will be updated by the institute.",
                Certification = "Details will be updated by the institute.",
                PracticalKnowledge = "Crime scene simulation, fingerprint pattern classification, forensic toxicology tests, and questioned document examination.",
                Eligibility = "Higher Secondary (10+2) in Science stream or equivalent recognized examination.",
                ImagePath = "/images/programs/forensic-science/forensic.jpg",
                IsActive = true
            },
            new Course
            {
                CourseId = 5,
                CourseCode = "MIC",
                CourseName = "B.Sc. Microbiology",
                Category = "Life Sciences",
                Description = "Investigate microscopic organisms, bacteria, viruses, fungi, culture techniques, and biological applications in healthcare and industry.",
                Duration = "3 Years (6 Semesters)",
                Fees = "Details will be updated by the institute.",
                Faculty = "Details will be updated by the institute.",
                Certification = "Details will be updated by the institute.",
                PracticalKnowledge = "Aseptic culturing, Gram staining, serial dilution, microbial isolation, and antibiotic sensitivity testing.",
                Eligibility = "Higher Secondary (10+2) with Biology or equivalent recognized examination.",
                ImagePath = "/images/programs/microbiology/microbiology.jpg",
                IsActive = true
            },
            new Course
            {
                CourseId = 6,
                CourseCode = "PHY",
                CourseName = "B.Sc. Physics",
                Category = "Physical Sciences",
                Description = "Study the fundamental laws of nature, mechanics, thermodynamics, optics, electromagnetism, and modern physical instrumentation.",
                Duration = "3 Years (6 Semesters)",
                Fees = "Details will be updated by the institute.",
                Faculty = "Details will be updated by the institute.",
                Certification = "Details will be updated by the institute.",
                PracticalKnowledge = "Optical bench experiments, spectrometer measurements, electrical circuit analysis, and digital multimeter calibration.",
                Eligibility = "Higher Secondary (10+2) with Physics and Mathematics or equivalent recognized examination.",
                ImagePath = "/images/programs/physics/physics.jpg",
                IsActive = true
            },
            new Course
            {
                CourseId = 7,
                CourseCode = "ZOO",
                CourseName = "B.Sc. Zoology",
                Category = "Life Sciences",
                Description = "Gain comprehensive insights into animal biology, physiological systems, taxonomy, genetics, and comparative vertebrate anatomy.",
                Duration = "3 Years (6 Semesters)",
                Fees = "Details will be updated by the institute.",
                Faculty = "Details will be updated by the institute.",
                Certification = "Details will be updated by the institute.",
                PracticalKnowledge = "Comparative histological slide examination, physiological measurement assays, taxonomic specimen analysis, and genetics simulations.",
                Eligibility = "Higher Secondary (10+2) with Biology or equivalent recognized examination.",
                ImagePath = "/images/programs/zoology/zoology.jpg",
                IsActive = true
            }
        );

        // Seed Default Admin
        modelBuilder.Entity<Admin>().HasData(
            new Admin
            {
                AdminId = 1,
                Username = "admin",
                Password = "admin123",
                FullName = "System Administrator",
                Email = "admin@ssv.edu.in",
                Role = "Admin"
            }
        );
    }
}
