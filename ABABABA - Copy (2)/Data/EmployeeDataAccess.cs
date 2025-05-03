using Microsoft.Data.SqlClient;
using WpfApp1.Model;
using WpfApp1.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.Data
{
    public static class EmployeeDataAccess
    {
        public static List<Member> GetEmployeesAsMembers()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    string query = "SELECT * FROM employees";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            var employees = new List<Employee>();
                            while (reader.Read())
                            {
                                employees.Add(new Employee
                                {
                                    UserId = Convert.ToInt32(reader["id"]),
                                    FirstName = reader["firstname"].ToString(),
                                    LastName = reader["lastname"].ToString(),
                                    Email = reader["email"].ToString(),
                                    PhoneNumber = reader["phonenumber"].ToString(),
                                    Department = reader["department"].ToString(),
                                    Role = reader["position"].ToString(),
                                    Salary = Convert.ToDecimal(reader["basicsalary"]),
                                    DateOfBirth = Convert.ToDateTime(reader["birthday"]),
                                    JoiningDate = Convert.ToDateTime(reader["joindate"]),
                                    Username = reader["username"].ToString(),
                                    Password = reader["password"].ToString(),
                                    Gender = reader["gender"].ToString(),
                                    Address = reader["address"].ToString()
                                });
                            }

                            // Map employees to members
                            return EmployeeMapper.MapEmployeesToMembers(employees);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving employees from the database.", ex);
            }
        }
    }
}
