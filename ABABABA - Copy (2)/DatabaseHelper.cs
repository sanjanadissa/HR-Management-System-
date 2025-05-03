using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    public static class DatabaseHelper
    {
        public static readonly string ConnectionString =
            @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\Asus\OneDrive\Desktop\final\asdf.mdf;Integrated Security=True;Connect Timeout=30";

        public static void InitializeDatabase()
        {
            using SqlConnection connection = new SqlConnection(ConnectionString);
            connection.Open();

            // Create employees table if it doesn't exist
            string createEmployeesTableQuery = @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='employees' AND xtype='U')
            BEGIN
                CREATE TABLE employees (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    firstname VARCHAR(50),
                    lastname VARCHAR(50),
                    address VARCHAR(100),
                    birthday DATE,
                    gender VARCHAR(10),
                    phonenumber VARCHAR(20),
                    department VARCHAR(50),
                    position VARCHAR(50),
                    joindate DATE,
                    email VARCHAR(100),
                    username VARCHAR(50),
                    password VARCHAR(100),
                    basicsalary DECIMAL(10,2)
                )
            END";

            SqlCommand cmd = new SqlCommand(createEmployeesTableQuery, connection);
            cmd.ExecuteNonQuery();

            string createDatabasesTableQuery = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='departments' AND xtype='U')
                BEGIN
                    CREATE TABLE departments (
                        id INT IDENTITY(1,1) PRIMARY KEY,
                        name NVARCHAR(100) NOT NULL
                    )
                END";

            cmd = new SqlCommand(createDatabasesTableQuery, connection);
            cmd.ExecuteNonQuery();

            string insertDepartmentsQuery = @"
    IF NOT EXISTS (SELECT * FROM departments WHERE name = 'HR')
        INSERT INTO departments (name) VALUES ('HR');
    IF NOT EXISTS (SELECT * FROM departments WHERE name = 'Finance')
        INSERT INTO departments (name) VALUES ('Finance');
    IF NOT EXISTS (SELECT * FROM departments WHERE name = 'IT')
        INSERT INTO departments (name) VALUES ('IT');
    IF NOT EXISTS (SELECT * FROM departments WHERE name = 'Marketing')
        INSERT INTO departments (name) VALUES ('Marketing');
    IF NOT EXISTS (SELECT * FROM departments WHERE name = 'Operations')
        INSERT INTO departments (name) VALUES ('Operations');
    IF NOT EXISTS (SELECT * FROM departments WHERE name = 'Sales')
        INSERT INTO departments (name) VALUES ('Sales');
";

            cmd = new SqlCommand(insertDepartmentsQuery, connection);
            cmd.ExecuteNonQuery();


            string sql = @"
        IF NOT EXISTS (
            SELECT * FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_NAME = 'leavereqs'
        )
        BEGIN
            CREATE TABLE [dbo].[leavereqs] (
                [id]             INT           IDENTITY (1, 1) NOT NULL,
                [employee_id]    INT           NULL,
                [name]           VARCHAR (MAX) NULL,
                [leavetype]      VARCHAR (50)  NULL,
                [numberofleaves] INT           NULL,
                [datesofleaves]  VARCHAR (MAX) NULL,
                PRIMARY KEY CLUSTERED ([id] ASC),
                FOREIGN KEY ([employee_id]) REFERENCES [dbo].[employees] ([id])
            );
        END";

            cmd = new SqlCommand(sql, connection);
            cmd.ExecuteNonQuery();

            string createTableQuery = @"
                        IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'appreq')
                        BEGIN
                            CREATE TABLE appreq (
                                id INT IDENTITY(1,1) PRIMARY KEY,
                                firstname NVARCHAR(100),
                                lastname NVARCHAR(100),
                                address NVARCHAR(200),
                                birthday DATE,
                                gender NVARCHAR(10),
                                phonenumber NVARCHAR(15),
                                department NVARCHAR(100),
                                position NVARCHAR(100),
                                joindate DATE,
                                email NVARCHAR(100),
                                password NVARCHAR(100)
                            )
                        END
                        ";

            cmd = new SqlCommand(createTableQuery, connection);
            cmd.ExecuteNonQuery();
        }
    }
}

