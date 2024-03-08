using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Project
{
    public partial class LecturesManagementSystem : Form
    {
        private SqlConnection conn;
        private SqlCommand cmd;
        private DataTable dataTable;
        public LecturesManagementSystem()
        {
            InitializeComponent();

            conn = new SqlConnection("Data source=(local);Initial Catalog=Project; Integrated Security=true");
            cmd = new SqlCommand();
            cmd.Connection = conn;

            dataTable = new DataTable();
        }


        private void LoadData(string tableName)
        {
            try
            {
                dataTable.Clear();
                DataTable emptyTable = new DataTable();
                dataGridView.DataSource = emptyTable;
                dataTable.Rows.Clear();
                dataTable.Columns.Clear();


                string query = $"SELECT * FROM {tableName}";

                conn.Open();

                cmd.CommandText = query;
                SqlDataReader reader = cmd.ExecuteReader();

                dataTable.Load(reader);

                reader.Close();

                dataGridView.DataSource = dataTable;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private void btnLectures_Click(object sender, EventArgs e)
        {
            btnDelete.Enabled = true;
            string tableName = "Lectures";
            LoadData(tableName);
            dataGridView.Tag = tableName;
        }

        private void btnTeachers_Click(object sender, EventArgs e)
        {
            btnDelete.Enabled = true;
            string tableName = "Teachers";
            LoadData(tableName);
            dataGridView.Tag = tableName;
        }

        private void btnSubjects_Click(object sender, EventArgs e)
        {
            btnDelete.Enabled = true;
            string tableName = "Subjects";
            LoadData(tableName);
            dataGridView.Tag = tableName;
        }

        private void btnClasses_Click(object sender, EventArgs e)
        {
            btnDelete.Enabled = false;
            string tableName = "Classes";
            LoadData(tableName);
            dataGridView.Tag = tableName;

        }

        private void btnClassesLectures_Click(object sender, EventArgs e)
        {
            btnDelete.Enabled = true;
            string tableName = "ClassLectures";
            LoadData(tableName);
            dataGridView.Tag = tableName;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                conn.Open();

                DataTable changes = dataTable.GetChanges();

                string tableName = dataGridView.Tag.ToString();

                if (tableName == "ClassLectures")
                {
                    foreach (DataRow row in changes.Rows)
                    {
                        int classId = Convert.ToInt32(row["ClassId"]);
                        int lectureId = Convert.ToInt32(row["LectureId"]);
                        string days = row["Days"].ToString();
                        TimeSpan startTime = TimeSpan.Parse(row["StartTime"].ToString());
                        TimeSpan endTime = TimeSpan.Parse(row["EndTime"].ToString());

                        // Check if the class lecture already exists
                        string checkQuery = $"SELECT COUNT(*) FROM ClassLectures WHERE ClassId = {classId} " +
                                            $"AND LectureId = {lectureId} AND Days = '{days}' " +
                                            $"AND StartTime = '{startTime}' AND EndTime = '{endTime}'";

                        SqlCommand checkCmd = new SqlCommand(checkQuery, conn);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (count > 0)
                        {
                            MessageBox.Show("This class lecture already exists.");
                            return;
                        }
                    }
                }

                if (tableName == "Lectures")
                {
                    foreach (DataRow row in changes.Rows)
                    {
                        int teacherId = Convert.ToInt32(row["TeacherId"]);
                        string days = row["Days"].ToString();

                        // Check if the teacher's available day matches the lecture day
                        string checkQuery = $"SELECT COUNT(*) FROM Teachers WHERE TeacherId = {teacherId} " +
                                            $"AND AvailableDays = '{days}'";

                        SqlCommand checkCmd = new SqlCommand(checkQuery, conn);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (count == 0)
                        {
                            MessageBox.Show("Teacher can have lectures only on their available days.");
                            return;
                        }
                    }
                }


                if (changes != null)
                {
                    string query = $"SELECT * FROM {tableName}";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                    SqlCommandBuilder builder = new SqlCommandBuilder(adapter);

                    // Configure the adapter to handle insert operations
                    builder.GetInsertCommand();

                    // Update the database with the changes
                    adapter.Update(changes);

                    // Refresh the DataTable to reflect the changes from the database
                    dataTable.Clear();
                    adapter.Fill(dataTable);
                }

                MessageBox.Show("Data saved successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedCells.Count > 0)
            {
                try
                {
                    // Get the selected cell
                    DataGridViewCell selectedCell = dataGridView.SelectedCells[0];

                    // Get the corresponding row
                    DataGridViewRow selectedRow = selectedCell.OwningRow;

                    // Get the currently active table name from the Tag property
                    string tableName = dataGridView.Tag.ToString();
                    int id = 0;
                    string subjectName = string.Empty;

                    if (tableName == "Teachers")
                    {
                        // Get the primary key value from the selected row
                        id = Convert.ToInt32(selectedRow.Cells["TeacherId"].Value);
                    }
                    else if (tableName == "Lectures")
                    {
                        id = Convert.ToInt32(selectedRow.Cells["LectureId"].Value);
                    }
                    else if (tableName == "ClassLectures")
                    {
                        id = Convert.ToInt32(selectedRow.Cells["ClassLectureId"].Value);
                    }
                    else if (tableName == "Subjects")
                    {
                        subjectName = selectedRow.Cells["SubjectName"].Value.ToString();
                    }

                    // Delete the corresponding record from the database
                    string query = string.Empty;
                    if (tableName == "Lectures")
                    {
                        // Check if the lecture is added to ClassLectures
                        SqlCommand checkCmd = new SqlCommand($"SELECT COUNT(*) FROM ClassLectures WHERE LectureId = @id", conn);
                        checkCmd.Parameters.AddWithValue("@id", id);
                        conn.Open();
                        int count = (int)checkCmd.ExecuteScalar();
                        conn.Close();

                        if (count > 0)
                        {
                            MessageBox.Show("This lecture is added to ClassLectures and cannot be deleted until it is removed from there.");
                            return; // Abort the deletion process
                        }

                        query = $"DELETE FROM {tableName} WHERE LectureId = @id";
                    }
                    else if (tableName == "Teachers")
                    {
                        query = $"DELETE FROM {tableName} WHERE TeacherId = @id";
                    }
                    else if (tableName == "Subjects")
                    {
                        query = $"DELETE FROM {tableName} WHERE SubjectName = @subjectName";
                    }
                    else if (tableName == "ClassLectures")
                    {
                        query = $"DELETE FROM {tableName} WHERE ClassLectureId = @id";
                    }

                    SqlCommand deleteCmd = new SqlCommand(query, conn);
                    conn.Open();

                    if (tableName == "Teachers" || tableName == "Lectures" || tableName == "ClassLectures")
                    {
                        deleteCmd.Parameters.AddWithValue("@id", id);
                    }
                    else if (tableName == "Subjects")
                    {
                        deleteCmd.Parameters.AddWithValue("@subjectName", subjectName);
                    }

                    deleteCmd.ExecuteNonQuery();
                    conn.Close();


                    // Remove the row from the DataGridView
                    dataGridView.Rows.Remove(selectedRow);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }
    }
}
