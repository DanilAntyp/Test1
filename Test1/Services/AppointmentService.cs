using System.Data;
using System.Data.SqlClient;
using Test1.Exceptions;
using Test1.Models;

namespace Test1.Services;



public class AppointmentService : IAppointmentService
{
    private readonly string _connectionString;

    public AppointmentService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<AppointmentDTO?> GetAppointmentByIdAsync(int appointmentId)
{
    const string sql = @"
    SELECT  a.date,p.first_name,p.last_name,p.date_of_birth,d.doctor_id,d.PWZ,s.name AS service_name, ap.service_fee
    FROM    Appointment  a
    JOIN    Patient  p  ON p.patient_id   = a.patient_id
    JOIN    Doctor  d  ON d.doctor_id    = a.doctor_id
    LEFT JOIN Appointment_Service ap ON ap.appointment_id = a.appointment_id   
    LEFT JOIN Service s  ON s.service_id   = ap.service_id         
    WHERE   a.appointment_id = @appointment_id;";

    await using var conn = new SqlConnection(_connectionString);
    await using var cmd  = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@appointment_id", appointmentId);

    await conn.OpenAsync();
    await using var reader = await cmd.ExecuteReaderAsync();

    AppointmentDTO? dto = null;

    while (await reader.ReadAsync())
    {
        if (dto is null)
        {
            dto = new AppointmentDTO
            {
                Date = reader.GetDateTime("Date"),
                Patient = new PatientDTO
                {
                    FirstName   = reader.GetString("first_name"),
                    LastName    = reader.GetString("last_name"),
                    DateOfBirth = reader.GetDateTime("date_of_birth")
                },
                Doctor = new DoctorDTO
                {
                    DoctorId = reader.GetInt32("doctor_id"),
                    PWZ    = reader.GetString("PWZ")
                }
            };
        }

        if (!reader.IsDBNull("service_name"))
        {
            dto.AppointmentServices.Add(new AppointmentServiceDTO
            {
                Name       = reader.GetString("service_name"),
                ServiceFee = reader.GetDecimal("service_fee")
            });
        }
    }

    return dto;    
}

   public async Task AddAppointmentAsync(CreateAppointmentDTO dto)
{
    await using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();
    await using var tx = await conn.BeginTransactionAsync();

    var exists = (int?)await new SqlCommand(
        "SELECT 1 FROM Appointment WHERE appointment_id = @id",
        conn, (SqlTransaction)tx)
    {
        Parameters = { new("@id", dto.AppointmentId) }
    }.ExecuteScalarAsync() != null;

    if (exists)
        throw new NotFoundException("not found doctor");
    
    var patientExists = (int?)await new SqlCommand(
        "SELECT 1 FROM Patient WHERE patient_id = @pid",
        conn, (SqlTransaction)tx)
    {
        Parameters = { new("@pid", dto.PatientId) }
    }.ExecuteScalarAsync() != null;

    if (!patientExists)
        throw new NotFoundException("not found patient ");
    
    var doctorIdObj = await new SqlCommand(
        "SELECT doctor_id FROM Doctor WHERE PWZ = @pwz",
        conn, (SqlTransaction)tx)
    {
        Parameters = { new("@pwz", dto.Pwz) }
    }.ExecuteScalarAsync();

    if (doctorIdObj is null)
        throw new NotFoundException("not found doctor");

    var doctorId = (int)doctorIdObj;

    var serviceNames = dto.Services.Select(s => s.ServiceName).Distinct().ToList();
    if (serviceNames.Count == 0)
        throw new ArgumentException("At least one service is required");

    var namesParam = string.Join(",", serviceNames.Select((_, i) => $"@n{i}"));
    var cmdFetchServices = new SqlCommand(
        $"SELECT service_id, name FROM Service WHERE name IN ({namesParam})",
        conn, (SqlTransaction)tx);

    for (int i = 0; i < serviceNames.Count; i++)
        cmdFetchServices.Parameters.AddWithValue($"@n{i}", serviceNames[i]);

    var idsByName = new Dictionary<string, int>();
    await using (var rdr = await cmdFetchServices.ExecuteReaderAsync())
        while (await rdr.ReadAsync())
            idsByName[rdr.GetString(1)] = rdr.GetInt32(0);

    var missing = serviceNames.Except(idsByName.Keys).ToList();
    if (missing.Any())
        throw new NotFoundException("not found service names");

    var cmdInsertAppt = new SqlCommand(
        @"INSERT INTO Appointment (appointment_id, patient_id, doctor_id, date)
          VALUES (@id, @pid, @did, SYSDATETIME())",
        conn, (SqlTransaction)tx);

    cmdInsertAppt.Parameters.AddWithValue("@id" , dto.AppointmentId);
    cmdInsertAppt.Parameters.AddWithValue("@pid", dto.PatientId);
    cmdInsertAppt.Parameters.AddWithValue("@did", doctorId);
    await cmdInsertAppt.ExecuteNonQueryAsync();

    const string insSql = @"INSERT INTO Appointment_Service
                            (appointment_id, service_id, service_fee)
                            VALUES (@aid, @sid, @fee)";

    foreach (var srv in dto.Services)
    {
        await new SqlCommand(insSql, conn, (SqlTransaction)tx)
        {
            Parameters =
            {
                new("@aid", dto.AppointmentId),
                new("@sid", idsByName[srv.ServiceName]),
                new("@fee", srv.ServiceFee)
            }
        }.ExecuteNonQueryAsync();
    }

    await tx.CommitAsync();
}
}