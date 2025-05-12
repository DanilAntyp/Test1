namespace Test1.Models;

public class AppointmentDTO
{
    public DateTime Date { get; set; }
    public PatientDTO Patient { get; set; } = new();
    public DoctorDTO Doctor { get; set; } = new();
    public List<AppointmentServiceDTO> AppointmentServices { get; set; } = new();
}

public class PatientDTO
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName  { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
}

public class DoctorDTO
{
    public int DoctorId { get; set; }
    public string PWZ    { get; set; } = string.Empty;
}

public class AppointmentServiceDTO         
{
    public string  Name       { get; set; } = string.Empty;
    public decimal ServiceFee { get; set; }
}