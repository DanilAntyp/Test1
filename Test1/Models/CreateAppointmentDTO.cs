namespace Test1.Models;


public class CreateAppointmentDTO
{
    public int AppointmentId { get; set; }
    public int PatientId     { get; set; }
    public string Pwz        { get; set; } = string.Empty;
    public List<CreateAppointmentServiceDTO> Services { get; set; } = new();
}

public class CreateAppointmentServiceDTO
{
    public string  ServiceName { get; set; } = string.Empty;
    public decimal ServiceFee  { get; set; }
}