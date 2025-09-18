using Microsoft.Data.SqlClient;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Schema;
using static Testing.MainWindow;

namespace Testing
{
    public partial class MainWindow : Window
    {
        private string _SQL_Server = "";
        private string _SQL_User = "";
        private string _SQL_Password = "";
        private string _SQL_connWarehouse = "";
        private string _SQL_connStoreManager = "";

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateConnectionString();
            //ReadDataFromDatabase();
            InitComboBoxes();
            setStores();
            setSizes();
            getStores();
            selectionCanvas.PreviewMouseDown += selectionCanvas_MouseDown;
            selectionCanvas.PreviewMouseMove += selectionCanvas_MouseMove;
            selectionCanvas.PreviewMouseUp += selectionCanvas_MouseUp;
        }

        private void UpdateConnectionString()
        {
            GetSQLSettings();
            _SQL_connWarehouse = $@"Server={_SQL_Server};Database=StoretecWarehouse; User Id=WarehouseAdmin;Password=SQL4Warehouse;TrustServerCertificate=True";
            _SQL_connStoreManager = $@"Server={_SQL_Server};Database=StoreManager; User Id=sa;Password=SQL4Admin;TrustServerCertificate=True";
        }


        private void InitComboBoxes()
        {

            comboPosX.Items.Add(0);
            comboPosY.Items.Add(0);
            // Von/Bis-Werte (1–30)
            for (int i = 1; i <= 30; i++)
            {
                AddItemsToComboBoxes(i, comboBoxVon, comboBoxBis, comboBoxVon2, comboBoxBis2, comboBoxVon3, comboBoxBis3, comboBoxVon4, comboBoxBis4, comboPosX, comboPosY);
            }

            anzahlSchubladen.Items.Add(4);
            anzahlSchubladen.Items.Add(6);

            artSimpli.Items.Add(552);
            artSimpli.Items.Add(720);

            comboSchrankvariante.Items.Add("A");
            comboSchrankvariante.Items.Add("B");
            int sSize = 75;
            for (int i = 0; i < 12; i++)
            {
                schachtSize.Items.Add(sSize);
                sSize += 75;
            }

            // Standard-Auswahl setzen
            SetDefaultSelections();
        }

        private void AddItemsToComboBoxes(int value, params ComboBox[] boxes)
        {
            foreach (var box in boxes)
            {
                box.Items.Add(value);
            }
        }

        private void SetDefaultSelections()
        {
            comboBoxVon.SelectedIndex = 0;
            comboBoxBis.SelectedIndex = 29;
            comboBoxSize.SelectedIndex = 0;

            comboBoxVon2.SelectedIndex = 0;
            comboBoxBis2.SelectedIndex = 29;
            comboBoxSize2.SelectedIndex = 0;

            comboBoxVon3.SelectedIndex = 0;
            comboBoxBis3.SelectedIndex = 29;
            comboBoxSize3.SelectedIndex = 0;

            comboBoxVon4.SelectedIndex = 0;
            comboBoxBis4.SelectedIndex = 29;
            comboBoxSize4.SelectedIndex = 0;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            int von = 0;
            int bis = 0;
            Sizes size = null;
            int sizeId1 = 0;
            int sizeId2 = 0;

            ComboBoxItem item = comboModel.SelectedItem as ComboBoxItem;
            string StoreName = item.Content.ToString();
            int type = (int)item.Tag;
            string addressdata = addressData.Text.ToString();

            int posX = int.Parse(positionX.Text);
            int posY = int.Parse(positionY.Text);

            if (addressdata == null || addressdata.Length == 0)
            {
                MessageBox.Show("Gib eine AddressData ein");
                return;
            }

            add_Model(StoreName, type, posX, posY);
            add_Sizes();

            int modelId = get_StoreId();
            int fachAnzahl = 0;

            foreach (var eintrag in FachInfo.FachListe)
            {
                von = eintrag.EbeneVon;
                bis = eintrag.EbeneBis;
                size = eintrag.Size;
                if (size.Description == "1er Fach klein")
                {
                    sizeId2 = get_SizeId("1er Fach groß");
                }
                if (size.Description == "2er Fach klein")
                {
                    sizeId2 = get_SizeId("2er Fach groß");
                }

                sizeId1 = get_SizeId(size.Description);
                if (size.Description == "1er Fach groß")
                {
                    sizeId1 = get_SizeId("1er Fach klein");
                    sizeId2 = get_SizeId(size.Description);
                }
                if (size.Description == "2er Fach groß")
                {
                    sizeId1 = get_SizeId("2er Fach klein");
                    sizeId2 = get_SizeId(size.Description);
                }
                fachAnzahl = size.faecherInEbene;

                add_to_Db(von, bis, sizeId1, sizeId2, modelId, fachAnzahl, size);
            }

            MessageBox.Show("in DB eingefügt");
            FachInfo.FachListe.Clear();
            RefreshListBox();
            getStores();
        }

        private void add_Model(string StoreName, int type, int posX, int posY)
        {
            UpdateConnectionString();
            string connectionString = _SQL_connWarehouse;
            string insertquery = "INSERT INTO Stores(StoreName, PosX, PosY, Height, Width, Type) values(@StoreName,@PosX,@PosY,0,0,@Type)";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    int inserted = 0;
                    connection.Open();


                    using (SqlCommand command = new SqlCommand(insertquery, connection))
                    {
                        command.Parameters.AddWithValue("@StoreName", StoreName);
                        command.Parameters.AddWithValue("@Type", type);
                        command.Parameters.AddWithValue("@PosX", posX);
                        command.Parameters.AddWithValue("@PosY", posY);
                        inserted += command.ExecuteNonQuery();
                    }
                }

                string sqlInsert = $"INSERT INTO Stores(StoreName, PosX, PosY, Height, Width, Type) VALUES ('{EscapeSql(StoreName)}', {posX}, {posY}, 0, 0, {type});{Environment.NewLine}";

                // Pfad zur SQL-Datei (kann angepasst werden)
                string filePath = @"C:\soft\configuration.sql";

                // In Datei anhängen
                File.AppendAllText(filePath, sqlInsert);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Einfügen des Stores. Exception:" + ex.ToString());
            }
        }

        private void GetSQLInstance(string Instance = @"localhost\SQLEXPRESS")
        {
            string SQLServer = Instance;
            if (!String.IsNullOrWhiteSpace(txtSQLinstance.Text))
            {
                SQLServer = @"localhost\" + txtSQLinstance.Text;
            }

            _SQL_Server = SQLServer;
        }

        private void GetSQLUser(string User = "WarehouseAdmin")
        {
            string SQLUser = User;
            if (!String.IsNullOrWhiteSpace(txtSQLuser.Text))
            {
                SQLUser = txtSQLuser.Text;
            }

            _SQL_User = SQLUser;
        }

        private void GetSQLPassword(string Password = "SQL4Warehouse")
        {
            string SQLPassword = Password;
            if (!String.IsNullOrWhiteSpace(txtSQLpassword.Text))
            {
                SQLPassword = txtSQLpassword.Text;
            }

            _SQL_Password = SQLPassword;
        }

        private void GetSQLSettings() {
            // from gui or default
            GetSQLInstance();
            GetSQLUser();
            GetSQLPassword();
        }

        private void add_Sizes()
        {
            UpdateConnectionString();
            string connectionString = _SQL_connWarehouse;
            string insertquery = @" IF NOT EXISTS (SELECT 1 FROM CompartmentSizes  WHERE Description = @Description AND AssociatedStore = @AssociatedStore)
            BEGIN
                INSERT INTO CompartmentSizes  (Description, Width, Height, Depth, AssociatedStore, CapacityCoefficient)
                VALUES  (@Description, @Width, @Height, @Depth, @AssociatedStore, @CapacityCoefficient)
            END";

            string filePath = @"C:\soft\configuration.sql";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(insertquery, connection))
                {
                    ComboBoxItem selectedModel = comboModel.SelectedItem as ComboBoxItem;
                    int gruppeId = (int)selectedModel.Tag;
                    var sizeListe = Sizes.SizeList.Where(z => z.GruppeID == gruppeId);
                    int inserted = 0;
                    foreach (var size in sizeListe)
                    {
                        command.Parameters.AddWithValue("@Description", size.Description);
                        command.Parameters.AddWithValue("@Width", size.Width);
                        command.Parameters.AddWithValue("@Height", size.Height);
                        command.Parameters.AddWithValue("@Depth", size.Depth);
                        command.Parameters.AddWithValue("@AssociatedStore", size.GruppeID);
                        command.Parameters.AddWithValue("@CapacityCoefficient", size.CapacityCoefficient);

                        inserted += command.ExecuteNonQuery();

                        string sqlText =$@"IF NOT EXISTS (SELECT 1 FROM CompartmentSizes WHERE Description = '{EscapeSql(size.Description)}' AND AssociatedStore = {size.GruppeID})
                            BEGIN
                                INSERT INTO CompartmentSizes (Description, Width, Height, Depth, AssociatedStore, CapacityCoefficient)
                                VALUES ('{EscapeSql(size.Description)}', {size.Width}, {size.Height}, {size.Depth}, {size.GruppeID}, {size.CapacityCoefficient})
                            END
                        ";

                        // Anhängen an configuration.sql
                        File.AppendAllText(filePath, sqlText + Environment.NewLine);


                        command.Parameters.Clear();
                    }
                }
            }
        }

        private int get_SizeId(string Description)
        {
            UpdateConnectionString();
            string connectionString = _SQL_connWarehouse;
            string query = "SELECT CompartmentSizeId from CompartmentSizes c where c.Description = @Description";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Description", Description);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read()) // Sicherstellen, dass ein Ergebnis existiert
                        {
                            return (int)reader["CompartmentSizeId"];
                        }
                        else
                        {
                            throw new Exception("Keine Größe mit dieser Beschreibung gefunden.");
                        }
                    }
                }
            }
        }

        private int get_StoreId()
        {
            UpdateConnectionString();
            string connectionString = _SQL_connWarehouse;
            string query = "Select top(1) StoreId  from Stores order by StoreId desc";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        reader.Read();
                        return (int)reader["StoreId"];
                    }
                }
            }
        }

        private void add_to_Db(int von, int bis, int size, int size2, int modelId, int fachAnzahl, Sizes sizes)
        {
            string posCode = "";

            UpdateConnectionString();
            string connectionString = _SQL_connWarehouse;
            string insertquery = "INSERT INTO Compartments (SizeId, PosX, PosY, PosZ, PosCode, Status, ReservationLimit, StoreId, AddressData) values (@SizeId, @PosX, @PosY, @PosZ, @PosCode, @Status, @ReservationLimit, @StoreId, @AddressData)";
            string Description = sizes.Description;

            int sizeId = 0;
            bool doppelSize = false;

            string Addressdata = addressData.Text.ToString();
            string addressdata = "{\"Port\":\"" + Addressdata + "\"}";

            if (Description == "1er Fach klein" || Description == "1er Fach groß" || Description == "2er Fach klein" || Description == "2er Fach groß")
            {
                doppelSize = true;
            }

            string sqlFilePath = @"C:\soft\configuration.sql";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    int inserted = 0;
                    using (StreamWriter sw = new StreamWriter(sqlFilePath, true))
                    {
                        for (int ebene = (int)von; ebene <= bis; ebene++)
                        {
                            if (Description == "2er Fach klein" || Description == "2er Fach groß" || Description == "6er Fach")
                            {
                                ebene++;
                            }
                            using (SqlCommand command = new SqlCommand(insertquery, connection))
                            {

                                for (int i = 1; i <= fachAnzahl; i++)
                                {
                                    if (Description == "3er Fach" || Description == "6er Fach")
                                    {
                                        i++;
                                        i++;
                                    }

                                    if (doppelSize == true && i % 3 != 0)
                                    {
                                        sizeId = size;
                                    }
                                    else if (doppelSize == true && i % 3 == 0)
                                    {
                                        sizeId = size2;
                                    }
                                    else
                                    {
                                        sizeId = size;
                                    }
                                    posCode = $"{modelId - 1}{ebene:D2}{i:D2}";
                                    command.Parameters.AddWithValue("@SizeId", sizeId);
                                    command.Parameters.AddWithValue("@PosX", i);
                                    command.Parameters.AddWithValue("@PosY", ebene);
                                    command.Parameters.AddWithValue("@PosZ", 0);
                                    command.Parameters.AddWithValue("@PosCode", posCode);
                                    command.Parameters.AddWithValue("@Status", 0);
                                    command.Parameters.AddWithValue("@ReservationLimit", 1);
                                    command.Parameters.AddWithValue("@StoreId", modelId);
                                    command.Parameters.AddWithValue("@AddressData", addressdata);

                                    inserted += command.ExecuteNonQuery();
                                    command.Parameters.Clear();

                                    string escapedAddressData = addressdata.Replace("\"", "\"\""); // oder eigene EscapeSql Methode

                                    string sqlLine = $"INSERT INTO Compartments (SizeId, PosX, PosY, PosZ, PosCode, Status, ReservationLimit, StoreId, AddressData) VALUES " +
                                        $"({sizeId}, {i}, {ebene}, 0, '{posCode}', 0, 1, {modelId}, '{escapedAddressData}');";

                                    sw.WriteLine(sqlLine);
                                }
                            }
                        }
                    }
                        
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("bitte gültige Werte auswählen");
                Debug.WriteLine("Fehler");
                Debug.WriteLine(ex);
            }
        }

        private void Duo_to_DB(object sender, RoutedEventArgs e)
        {
            ComboBoxItem item = comboModel.SelectedItem as ComboBoxItem;
            string StoreName = item.Content.ToString();
            int type = (int)item.Tag;
            string data = addressData.Text.ToString();
            int posX = int.Parse(positionX.Text);
            int posY = int.Parse(positionY.Text);
            if (data == null || data.Length == 0)
            {
                MessageBox.Show("Gib eine AddressData ein");
                return;
            }
            string addressdata = "{\"Port\":\"" + data + "\"}";

            add_Model(StoreName, type, posX, posY);
            add_Sizes();

            string posCode = "";
            int anzSchubladen = (int)anzahlSchubladen.SelectedItem;
            int size = 0;

            UpdateConnectionString();
            string connectionString = _SQL_connWarehouse;
            string insertquery = "INSERT INTO Compartments (SizeId, PosX, PosY, PosZ, PosCode, Status, ReservationLimit, StoreId, AddressData) values (@SizeId, @PosX, @PosY, @PosZ, @PosCode, @Status, @ReservationLimit, @StoreId, @AddressData)";

            int modelId = get_StoreId();
            int sizeId1 = 0;
            int sizeId2 = 0;
            int schublade1 = 0;
            int schublade2 = 0;

            int anzahl = 5;

            string filePath = @"C:\soft\configuration.sql";

            foreach (var eintrag in Sizes.SizeList.Where(f => f.Description == "DUO.Comp.Small"))
            {
                sizeId1 = get_SizeId(eintrag.Description);
            }
            foreach (var eintrag in Sizes.SizeList.Where(f => f.Description == "DUO.Comp.Large"))
            {
                sizeId2 = get_SizeId(eintrag.Description);
            }
            foreach (var eintrag in Sizes.SizeList.Where(f => f.Description == "DUO.Drawer.Large"))
            {
                schublade1 = get_SizeId(eintrag.Description);
            }
            foreach (var eintrag in Sizes.SizeList.Where(f => f.Description == "DUO.Drawer.Small"))
            {
                schublade2 = get_SizeId(eintrag.Description);
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    int inserted = 0;

                    for (int ebene = (int)16; ebene <= 30; ebene++)
                    {
                        using (SqlCommand command = new SqlCommand(insertquery, connection))
                        {

                            for (int i = 1; i <= 24; i++)
                            {
                                if (i % 2 == 1)
                                {
                                    size = sizeId1;
                                }
                                else
                                {
                                    size = sizeId2;
                                }
                                posCode = $"{modelId - 1}{ebene:D2}{i:D2}";
                                command.Parameters.AddWithValue("@SizeId", size);
                                command.Parameters.AddWithValue("@PosX", i);
                                command.Parameters.AddWithValue("@PosY", ebene);
                                command.Parameters.AddWithValue("@PosZ", 0);
                                command.Parameters.AddWithValue("@PosCode", posCode);
                                command.Parameters.AddWithValue("@Status", 0);
                                command.Parameters.AddWithValue("@ReservationLimit", 1);
                                command.Parameters.AddWithValue("@StoreId", modelId);
                                command.Parameters.AddWithValue("@AddressData", addressdata);

                                inserted += command.ExecuteNonQuery();
                                command.Parameters.Clear();

                                string sqlLine = $"INSERT INTO Compartments (SizeId, PosX, PosY, PosZ, PosCode, Status, ReservationLimit, StoreId, AddressData) " +
                                         $"VALUES ({size}, {i}, {ebene}, 0, '{EscapeSql(posCode)}', 0, 1, {modelId}, '{EscapeSql(addressdata)}');";
                                File.AppendAllText(filePath, sqlLine + Environment.NewLine);
                            }
                        }
                    }

                    if ((int)anzahlSchubladen.SelectedItem == 6)
                    {
                        anzahl = 7;
                    }
                    using (SqlCommand command = new SqlCommand(insertquery, connection))
                    {

                        for (int i = 1; i < 3; i++)
                        {
                            posCode = $"{modelId - 1}{i:D2}{0:D2}";
                            command.Parameters.AddWithValue("@SizeId", schublade1);
                            command.Parameters.AddWithValue("@PosX", 1);
                            command.Parameters.AddWithValue("@PosY", i);
                            command.Parameters.AddWithValue("@PosZ", 0);
                            command.Parameters.AddWithValue("@PosCode", posCode);
                            command.Parameters.AddWithValue("@Status", 0);
                            command.Parameters.AddWithValue("@ReservationLimit", 1);
                            command.Parameters.AddWithValue("@StoreId", modelId);
                            command.Parameters.AddWithValue("@AddressData", addressdata);

                            inserted += command.ExecuteNonQuery();
                            command.Parameters.Clear();

                            string sqlLine = $"INSERT INTO Compartments (SizeId, PosX, PosY, PosZ, PosCode, Status, ReservationLimit, StoreId, AddressData) " +
                                     $"VALUES ({schublade1}, 1, {i}, 0, '{EscapeSql(posCode)}', 0, 1, {modelId}, '{EscapeSql(addressdata)}');";
                            File.AppendAllText(filePath, sqlLine + Environment.NewLine);

                        }
                        for (int i = 3; i < anzahl; i++)
                        {
                            posCode = $"{modelId - 1}{i:D2}{0:D2}";
                            command.Parameters.AddWithValue("@SizeId", schublade2);
                            command.Parameters.AddWithValue("@PosX", 1);
                            command.Parameters.AddWithValue("@PosY", i);
                            command.Parameters.AddWithValue("@PosZ", 0);
                            command.Parameters.AddWithValue("@PosCode", posCode);
                            command.Parameters.AddWithValue("@Status", 0);
                            command.Parameters.AddWithValue("@ReservationLimit", 1);
                            command.Parameters.AddWithValue("@StoreId", modelId);
                            command.Parameters.AddWithValue("@AddressData", addressdata);

                            inserted += command.ExecuteNonQuery();
                            command.Parameters.Clear();

                            string sqlLine = $"INSERT INTO Compartments (SizeId, PosX, PosY, PosZ, PosCode, Status, ReservationLimit, StoreId, AddressData) " +
                                     $"VALUES ({schublade2}, 1, {i}, 0, '{EscapeSql(posCode)}', 0, 1, {modelId}, '{EscapeSql(addressdata)}');";
                            File.AppendAllText(filePath, sqlLine + Environment.NewLine);

                        }
                    }
                    MessageBox.Show("Duo in DB eingefügt");
                    getStores();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("bitte gültige Werte auswählen");
                Debug.WriteLine("Fehler");
                Debug.WriteLine(ex);
            }


        }

        private void simpli_to_Db(object sender, RoutedEventArgs e)
        {
            ComboBoxItem item = comboModel.SelectedItem as ComboBoxItem;
            string StoreName = item.Content.ToString();
            int type = (int)item.Tag;
            string data = addressData.Text.ToString();
            int posX = int.Parse(positionX.Text);
            int posY = int.Parse(positionY.Text);
            int size = 0;
            string posCode = "";
            int sizeId1 = 0;
            int sizeId2 = 0;


            if (data == null || data.Length == 0)
            {
                MessageBox.Show("Gib eine AddressData ein");
                return;
            }
            string addressdata = "{\"Port\":\"" + data + "\"}";

            add_Model(StoreName, type, posX, posY);
            add_Sizes();

            int modelId = get_StoreId();

            UpdateConnectionString();
            string connectionString = _SQL_connWarehouse;
            string insertquery = "INSERT INTO Compartments (SizeId, PosX, PosY, PosZ, PosCode, Status, ReservationLimit, StoreId, AddressData) values (@SizeId, @PosX, @PosY, @PosZ, @PosCode, @Status, @ReservationLimit, @StoreId, @AddressData)";

            string filePath = @"C:\soft\configuration.sql";

            foreach (var eintrag in Sizes.SizeList.Where(f => f.Description == "simpli klein"))
            {
                sizeId1 = get_SizeId(eintrag.Description);
            }
            foreach (var eintrag in Sizes.SizeList.Where(f => f.Description == "simpli groß"))
            {
                sizeId2 = get_SizeId(eintrag.Description);
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    for (int ebene = (int)1; ebene <= 30; ebene++)
                    {
                        if ((int)artSimpli.SelectedItem == 552 && ebene >= 17)
                        {
                            ebene++;
                            sizeId1 = get_SizeId("simpli 2er klein");
                            sizeId2 = get_SizeId("simpli 2er groß");
                        }
                        using (SqlCommand command = new SqlCommand(insertquery, connection))
                        {

                            for (int i = 1; i <= 24; i++)
                            {
                                if (i % 2 == 1)
                                {
                                    size = sizeId1;
                                }
                                else
                                {
                                    size = sizeId2;
                                }
                                posCode = $"{modelId - 1}{ebene:D2}{i:D2}";
                                command.Parameters.AddWithValue("@SizeId", size);
                                command.Parameters.AddWithValue("@PosX", i);
                                command.Parameters.AddWithValue("@PosY", ebene);
                                command.Parameters.AddWithValue("@PosZ", 0);
                                command.Parameters.AddWithValue("@PosCode", posCode);
                                command.Parameters.AddWithValue("@Status", 0);
                                command.Parameters.AddWithValue("@ReservationLimit", 1);
                                command.Parameters.AddWithValue("@StoreId", modelId);
                                command.Parameters.AddWithValue("@AddressData", addressdata);

                                command.ExecuteNonQuery();
                                command.Parameters.Clear();

                                string sqlInsert = $"INSERT INTO Compartments (SizeId, PosX, PosY, PosZ, PosCode, Status, ReservationLimit, StoreId, AddressData) VALUES " + $"({size}, {i}, {ebene}, 0, '{EscapeSql(posCode)}', 0, 1, {modelId}, '{EscapeSql(addressdata)}');";

                                File.AppendAllText(filePath, sqlInsert + Environment.NewLine);
                            }
                        }
                    }
                    MessageBox.Show("Simpli in Db eingefügt");
                    getStores();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Einfügen: " + ex.Message);
            }
        }

        private void comboModel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem selectedModel = comboModel.SelectedItem as ComboBoxItem;
            string Model = selectedModel.Content.ToString();
            int gruppeId = (int)selectedModel.Tag;
            var sizeListe = Sizes.SizeList.Where(z => z.GruppeID == gruppeId);

            comboBoxSize.Items.Clear();
            comboBoxSize2.Items.Clear();
            comboBoxSize3.Items.Clear();
            comboBoxSize4.Items.Clear();
            foreach (var size in sizeListe)
            {
                if (size.Description != "1er Fach groß" && size.Description != "2er Fach groß")
                {
                    comboBoxSize.Items.Add(new ComboBoxItem
                    {
                        Content = size.Description,
                        Tag = size
                    });
                    comboBoxSize2.Items.Add(new ComboBoxItem
                    {
                        Content = size.Description,
                        Tag = size
                    });
                    comboBoxSize3.Items.Add(new ComboBoxItem
                    {
                        Content = size.Description,
                        Tag = size
                    });
                    comboBoxSize4.Items.Add(new ComboBoxItem
                    {
                        Content = size.Description,
                        Tag = size
                    });
                }
            }

            if (comboModel.SelectedItem != null && (Model == "Pro2160" || Model == "Pro1080" || Model == "ArnoPro2160" || Model == "ArnoPro1080"))
            {
                panel.Visibility = Visibility.Visible; // "show"
                panel21.Visibility = Visibility.Collapsed;
                panelSimpli.Visibility = Visibility.Collapsed;
            }
            else if (Model == "Arno Duo")
            {
                panel.Visibility = Visibility.Collapsed;
                panel21.Visibility = Visibility.Visible;
                panelSimpli.Visibility = Visibility.Collapsed;
                panel2.Visibility = Visibility.Collapsed;
                panel3.Visibility = Visibility.Collapsed;
                panel4.Visibility = Visibility.Collapsed;
            }
            else
            {
                panelSimpli.Visibility = Visibility.Visible;
                panel21.Visibility = Visibility.Collapsed;
                panel.Visibility = Visibility.Collapsed;
                panel2.Visibility = Visibility.Collapsed;
                panel3.Visibility = Visibility.Collapsed;
                panel4.Visibility = Visibility.Collapsed;

            }

        }

        private void boxVonBis_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxVon.SelectedItem is int von && comboBoxBis.SelectedItem is int bis &&
                comboBoxVon2.SelectedItem is int von2 && comboBoxBis2.SelectedItem is int bis2 &&
                comboBoxVon3.SelectedItem is int von3 && comboBoxBis3.SelectedItem is int bis3 &&
                comboBoxVon4.SelectedItem is int von4 && comboBoxBis4.SelectedItem is int bis4)
            {
                int differenz1 = bis - von;
                int differenz2 = bis2 - von2;
                int differenz3 = bis3 - von3;
                int differenz4 = bis4 - von4;

                if (bis == 30)
                {
                    panel2.Visibility = Visibility.Hidden;
                    panel3.Visibility = Visibility.Hidden;
                    panel4.Visibility = Visibility.Hidden;
                    return;
                }
                comboBoxVon2.SelectedIndex = bis;
                panel2.Visibility = Visibility.Visible;

                if (bis2 == 30)
                {
                    panel3.Visibility = Visibility.Hidden;
                    panel4.Visibility = Visibility.Hidden;
                    panel5.Visibility = Visibility.Hidden;
                    return;
                }
                if (von2 > bis2)
                {
                    panel5.Visibility = Visibility.Visible;
                    panel3.Visibility = Visibility.Hidden;
                    panel4.Visibility = Visibility.Hidden;
                    return;
                }
                panel5.Visibility = Visibility.Hidden;
                comboBoxVon3.SelectedIndex = bis2;
                panel3.Visibility = Visibility.Visible;

                if (bis3 == 30)
                {
                    panel4.Visibility = Visibility.Hidden;
                    panel5.Visibility = Visibility.Hidden;
                    return;
                }
                if (von3 > bis3)
                {
                    panel5.Visibility = Visibility.Visible;
                    panel4.Visibility = Visibility.Hidden;
                    return;
                }
                panel5.Visibility = Visibility.Hidden;
                comboBoxVon4.SelectedIndex = bis3;
                panel4.Visibility = Visibility.Visible;

                if (bis4 != 30)
                {
                    panel5.Visibility = Visibility.Visible;
                }


            }
        }

        private void getStores()
        {
            UpdateConnectionString();
            string connectionString = _SQL_connWarehouse;
            string query = "SELECT StoreId, StoreName, Type FROM Stores";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {

                        comboStores.Items.Clear();
                        while (reader.Read())
                        {
                            int StoreId = (int)reader["StoreId"];
                            string StoreName = reader["StoreName"].ToString();
                            int type = (int)reader["Type"];

                            StoreModelInfo eintrag = new StoreModelInfo { StoreId = StoreId, StoreName = StoreName, Type = type };

                            comboStores.Items.Add(new ComboBoxItem
                            {
                                Content = StoreName,
                                Tag = StoreId
                            });
                        }
                    }
                }

                Debug.WriteLine("Fertig.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Zugriff auf die Datenbank:");
                Console.WriteLine(ex.Message);
            }
        }

        private void delete_Compartments()
        {
            var selectedStore = comboStores.SelectedItem as ComboBoxItem;
            int storeId = (int)selectedStore.Tag;
            int PosX = int.Parse(comboPosX.Text.ToString());
            int PosY = int.Parse(comboPosY.Text.ToString());

            UpdateConnectionString();
            string connectionString = _SQL_connWarehouse;
            string deleteQuery = @"DELETE c from Compartments c inner join Stores s on c.StoreId = s.StoreId where s.StoreId = @storeId and s.PosX=@PosX and s.PosY = @PosY";
            string selectQuery = @"Select Count(StoreId) from Compartments c inner join StockItems s on c.CompartmentId = s.CompartmentId where StoreId = @storeId";

            string filePath = @"C:\soft\configuration.sql";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    int totalBeforeDelete = 0;
                    using (SqlCommand command = new SqlCommand(selectQuery, connection))
                    {
                        command.Parameters.AddWithValue("@storeId", storeId);
                        totalBeforeDelete = (int)command.ExecuteScalar(); 
                    }
                    if (totalBeforeDelete != 0)
                    {
                        MessageBox.Show("Manche Fächer haben noch Artikel. Bitte davor löschen");
                        return;
                    }
                    int rowsAffected = 0;
                    using (SqlCommand command = new SqlCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@storeId", storeId);
                        command.Parameters.AddWithValue("@PosX", PosX);
                        command.Parameters.AddWithValue("@PosY", PosY);
                        rowsAffected = command.ExecuteNonQuery();

                        MessageBox.Show($"{rowsAffected} Zeile gelöscht");

                        string sqlDelete = $"DELETE c FROM Compartments c INNER JOIN Stores s ON c.StoreId = s.StoreId " +
                                  $"WHERE s.StoreId = {storeId} AND s.PosX = {PosX} AND s.PosY = {PosY};";

                        File.AppendAllText(filePath, sqlDelete + Environment.NewLine);
                    }


                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Löschen:/n" + ex.Message);
            }
        }

        private void delete_Model()
        {
            var selectedStore = comboStores.SelectedItem as ComboBoxItem;
            int storeId = (int)selectedStore.Tag;

            UpdateConnectionString();
            string connectionString = _SQL_connWarehouse;
            string deleteQuery = @"Delete from Stores where StoreId = @storeId";

            string resetQuery = "";

            string selectQuery = "Select top(1) StoreId from Stores order by StoreId DESC";

            int id = 0;

            string filePath = @"C:\soft\configuration.sql";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@storeId", storeId);
                        command.ExecuteNonQuery();
                    }
                    using (SqlCommand command = new SqlCommand(selectQuery, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                id = (int)reader["StoreId"];
                            }

                            resetQuery = $"DBCC CHECKIDENT('Stores', RESEED, {id})";
                        }
                    }
                    using (SqlCommand command = new SqlCommand(resetQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    string sqlToWrite = $"DELETE FROM Stores WHERE StoreId = {storeId};{Environment.NewLine}" +
                                $"{resetQuery};{Environment.NewLine}";

                    File.AppendAllText(filePath, sqlToWrite + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Löschen des Stores");
            }

        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            delete_Compartments();
            delete_Model();
            getStores();
        }

        private void Add_List(object sender, RoutedEventArgs e)
        {
            List<ComboBox> comboBoxVonList = new List<ComboBox> { comboBoxVon, comboBoxVon2, comboBoxVon3, comboBoxVon4 };
            List<ComboBox> comboBoxBisList = new List<ComboBox> { comboBoxBis, comboBoxBis2, comboBoxBis3, comboBoxBis4 };
            List<ComboBox> comboBoxSizeList = new List<ComboBox> { comboBoxSize, comboBoxSize2, comboBoxSize3, comboBoxSize4 };

            Button clickedButton = sender as Button;
            string tag = clickedButton?.Tag?.ToString();
            int zahl = int.Parse(tag);
            int von = 0;
            int bis = 0;
            Sizes sizes = null;
            int size = 0;


            if (zahl >= 0 && zahl < comboBoxVonList.Count && zahl < comboBoxBisList.Count && zahl < comboBoxSizeList.Count)
            {
                ComboBox boxVon = comboBoxVonList[zahl];
                ComboBox boxBis = comboBoxBisList[zahl];
                ComboBox boxSize = comboBoxSizeList[zahl];
                ComboBoxItem sizeItem = boxSize.SelectedItem as ComboBoxItem;

                von = (int)boxVon.SelectedItem;
                bis = (int)boxBis.SelectedItem;
                sizes = (Sizes)sizeItem.Tag;
            }
            FachInfo.Hinzufügen(zahl, von, bis, sizes);

            RefreshListBox();
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            string tag = clickedButton?.Tag?.ToString();
            int zeile = int.Parse(tag);


            FachInfo.Entfernen(zeile);
            RefreshListBox();
        }

        private void RefreshListBox()
        {
            var eintreage = FachInfo.FachListe.Cast<FachInfo>().OrderBy(f => f.Zeile);
            listBoxAdd.Items.Clear();
            foreach (var eintrag in eintreage)
            {
                listBoxAdd.Items.Add(eintrag);
            }
        }

        private void setSizes()
        {
            Sizes.Hinzufügen("DUO.Comp.Small", 5, 87, 53, 195, 0.6f, 24);
            Sizes.Hinzufügen("DUO.Comp.Large", 5, 87, 53, 195, 1, 24);
            Sizes.Hinzufügen("DUO.Drawer.Small", 5, 612, 100, 612, 1, 24);
            Sizes.Hinzufügen("DUO.Drawer.Large", 5, 612, 200, 612, 1, 24);
            Sizes.Hinzufügen("1er Fach klein", 1, 34, 52, 205, 0.6f, 72);
            Sizes.Hinzufügen("1er Fach groß", 1, 34, 52, 205, 1, 72);
            Sizes.Hinzufügen("2er Fach klein", 1, 34, 104, 205, 0.6f, 72);
            Sizes.Hinzufügen("2er Fach groß", 1, 34, 104, 205, 1, 72);
            Sizes.Hinzufügen("3er Fach", 1, 110, 52, 205, 1, 72);
            Sizes.Hinzufügen("6er Fach", 1, 110, 104, 205, 1, 72);
            Sizes.Hinzufügen("simpli klein", 8, 87, 53, 195, 0.6f, 24);
            Sizes.Hinzufügen("simpli groß", 8, 87, 53, 195, 1, 24);
            Sizes.Hinzufügen("simpli 2er klein", 8, 87, 106, 195, 0.6f, 24);
            Sizes.Hinzufügen("simpli 2er groß", 8, 87, 106, 195, 1, 24);
        }

        private void setStores()
        {
            comboModel.Items.Clear();
            Stores.Hinzufügen("Arno Duo", 5);
            Stores.Hinzufügen("Pro2160", 1);
            Stores.Hinzufügen("Pro1080", 1);
            Stores.Hinzufügen("ArnoPro2160", 1);
            Stores.Hinzufügen("ArnoPro1080", 1);
            Stores.Hinzufügen("Simpli", 8);

            foreach (var store in Stores.StoreList)
            {
                comboModel.Items.Add(new ComboBoxItem
                {
                    Content = store.StoreName,
                    Tag = store.SizeGruppeId
                });
            }
        }

        public class FachInfo
        {
            public int Zeile { get; set; }
            public int EbeneVon { get; set; }
            public int EbeneBis { get; set; }
            public Sizes Size { get; set; }

            public static List<FachInfo> FachListe { get; private set; } = new List<FachInfo>();

            public static void Hinzufügen(int zeile, int von, int bis, Sizes size)
            {
                var neuesFach = new FachInfo
                {
                    Zeile = zeile,
                    EbeneVon = von,
                    EbeneBis = bis,
                    Size = size
                };

                FachListe.Add(neuesFach);
            }

            public static void Entfernen(int zeile)
            {
                FachListe.RemoveAll(f => f.Zeile == zeile);
            }


            public override string ToString()
            {
                return $"Von: {EbeneVon}, Bis: {EbeneBis}, Size: {Size.Description}";
            }
        }
        public class StoreModelInfo
        {
            public int StoreId { get; set; }
            public string StoreName { get; set; }
            public int Type { get; set; }

            public static List<StoreModelInfo> ModellListe { get; private set; } = new List<StoreModelInfo>();

            public static void Hinzufügen(int storeId, string storeName, int type)
            {
                // Bereits vorhandenes Modell mit gleicher ID entfernen
                Entfernen(storeId);

                var neuesModell = new StoreModelInfo
                {
                    StoreId = storeId,
                    StoreName = storeName,
                    Type = type
                };

                ModellListe.Add(neuesModell);
            }


            public static void Entfernen(int storeId)
            {
                ModellListe.RemoveAll(m => m.StoreId == storeId);
            }


            public override string ToString()
            {
                return $"ID: {StoreId}, Name: {StoreName}, Type: {Type}";
            }
        }

        public class Sizes
        {

            public string Description { get; set; }

            public int Width { get; set; }
            public int Height { get; set; }
            public int Depth { get; set; }
            public float CapacityCoefficient { get; set; }
            public int GruppeID { get; set; }
            public int faecherInEbene { get; set; }

            public static List<Sizes> SizeList { get; private set; } = new List<Sizes>();

            public static void Hinzufügen(string Description, int GruppeId, int Width, int Height, int Depth, float CapacityCoefficient, int FaecheInEbene)
            {
                var size = new Sizes
                {
                    Description = Description,
                    Width = Width,
                    Height = Height,
                    Depth = Depth,
                    CapacityCoefficient = CapacityCoefficient,
                    GruppeID = GruppeId,
                    faecherInEbene = FaecheInEbene
                };

                SizeList.Add(size);
            }
        }
        public class Stores
        {
            public string StoreName { get; set; }
            public int SizeGruppeId { get; set; }

            public static List<Stores> StoreList { get; private set; } = new List<Stores>();

            public static void Hinzufügen(string storeName, int SizeGruppeId)
            {

                var neuesStore = new Stores
                {
                    StoreName = storeName,
                    SizeGruppeId = SizeGruppeId
                };

                StoreList.Add(neuesStore);
            }
        }

        public class Buttons()
        {
            public string content { get; set; }
            public int spalte { get; set; }
            public int zeile { get; set; }
            public int size { get; set; }
            public string board { get; set; }

            public Button UIBtn { get; set; }

            public static List<Buttons> buttonsList { get; private set; } = new List<Buttons>();
            public static void Hinzufuegen(Button btn, int col, int row, int size, string board)
            {
                var button = new Buttons
                {
                    content = btn.Content.ToString(),
                    spalte = col + 1,
                    zeile = row + 1,
                    UIBtn = btn,
                    size = size,
                    board = board
                };

                buttonsList.Add(button);
            }
        }

        private List<Button> ausgewaehlteButtons = new List<Button>();

        public void lockerArt_Change(object sender, SelectionChangedEventArgs e)
        {

            if (comboSchrankvariante.SelectedItem.ToString() == "B")
            {
                schacht3.Visibility = Visibility.Hidden;
                schacht4.Visibility = Visibility.Hidden;
                schacht3.Text = 0.ToString();
                schacht4.Text = 0.ToString();
                return;
            }

            schacht3.Visibility = Visibility.Visible;
            schacht4.Visibility = Visibility.Visible;
        }

        private void ErzeugeMatrix(object sender, RoutedEventArgs e)
        {
            Buttons.buttonsList.Clear();
            matrixGrid.Children.Clear();
            matrixGrid.ColumnDefinitions.Clear();
            matrixGrid.RowDefinitions.Clear();

            var textBoxes = new List<TextBox> { schacht1, schacht2, schacht3, schacht4 };

            int maxZeilen = 50;

            // Spalten definieren
            for (int col = 0; col < textBoxes.Count; col++)
            {
                matrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                if (int.TryParse(textBoxes[col].Text, out int zeilen))
                {
                    if (zeilen > maxZeilen)
                        maxZeilen = zeilen;
                }
            }

            // Zeilen definieren (für max. benötigte Zeilen)
            for (int i = 0; i < maxZeilen; i++)
            {
                matrixGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            // Buttons einsetzen
            for (int col = 0; col < textBoxes.Count; col++)
            {
                if (int.TryParse(textBoxes[col].Text, out int zeilen))
                {
                    for (int row = 0; row < zeilen; row++)
                    {
                        Button btn = new Button
                        {
                            Content = $"[{col},{row}] (75)",
                            Width = 60,
                            Height = 30,
                            Margin = new Thickness(2),
                            Tag = new Tuple<int, int>(col, row)
                        };
                        btn.Click += MatrixButton_Click;
                        Buttons.Hinzufuegen(btn, col, row, 75, "com1");
                        Grid.SetColumn(btn, col);
                        Grid.SetRow(btn, row);
                        matrixGrid.Children.Add(btn);
                    }
                }
            }

            matrixGrid.UpdateLayout();

            ausgewaehlteButtons.Clear();
            listAusgewaehlteButtons.Items.Clear();

            Dispatcher.InvokeAsync(() =>
            {
                // Canvas-Größe ggf. erweitern
                selectionCanvas.Width = Math.Max(matrixGrid.ActualWidth + 100, scrollViewer.ActualWidth);
                selectionCanvas.Height = Math.Max(matrixGrid.ActualHeight + 100, scrollViewer.ActualHeight);

                // Matrix zentrieren
                double left = (selectionCanvas.ActualWidth - matrixGrid.ActualWidth) / 2;
                //double top = (selectionCanvas.ActualHeight - matrixGrid.ActualHeight) / 2;

                Canvas.SetLeft(matrixGrid, left);
                //Canvas.SetTop(matrixGrid, top);
            });
        }

        private void MatrixButton_Click(object sender, RoutedEventArgs e)
        {
            string content = "";
            if (sender is Button btn)
            {
                // Ein/Auswahl umschalten
                if (ausgewaehlteButtons.Contains(btn))
                {
                    ausgewaehlteButtons.Remove(btn);

                    // Hol das zugehörige Datenobjekt
                    var datenButton = Buttons.buttonsList.FirstOrDefault(b => b.UIBtn == btn);
                    if (datenButton != null && !string.IsNullOrEmpty(datenButton.board))
                    {
                        // Finde die zugehörige Farbe aus boardEintraege
                        var boardEintrag = boardEintraege.FirstOrDefault(b => b.Name == datenButton.board);
                        if (boardEintrag != null)
                        {
                            btn.Background = boardEintrag.Farbe;
                        }
                        else
                        {
                            btn.Background = Brushes.LightGray;
                        }
                    }
                    else
                    {
                        btn.Background = Brushes.LightGray;
                    }
                }
                else
                {
                    ausgewaehlteButtons.Add(btn);
                    btn.Background = Brushes.Blue;
                }

                listAusgewaehlteButtons.Items.Clear();
                foreach (var b in ausgewaehlteButtons)
                {
                    content += b.Content.ToString();
                    content += " ; ";
                    // Button direkt als Item
                }
                listAusgewaehlteButtons.Items.Add(content);
            }
        }

        private void add_Board(object sender, RoutedEventArgs e)
        {
            string board = txtRouterIF.Text.Trim();
            if (string.IsNullOrEmpty(board)) return;

            if (!boardColors.ContainsKey(board))
            {
                Brush farbe = verfügbareFarben[boardColors.Count % verfügbareFarben.Count];
                boardColors[board] = farbe;
            }

            if (boardEintraege.Any(b => b.Name == board)) return;

            boardEintraege.Add(new BoardEntry
            {
                Name = board,
                Farbe = boardColors[board]
            });

            listBoards.ItemsSource = null;              // Reset
            listBoards.ItemsSource = boardEintraege;
        }

        private void remove_Board(object sender, RoutedEventArgs e)
        {
            if (listBoards.SelectedItem is BoardEntry selectedBoard)
            {
                boardEintraege.Remove(selectedBoard);

                // Entferne auch die Farbe aus dem Dictionary, falls du möchtest
                if (boardColors.ContainsKey(selectedBoard.Name))
                    boardColors.Remove(selectedBoard.Name);

                // Buttons aktualisieren, die dieses Board noch nutzen
                foreach (var btn in Buttons.buttonsList)
                {
                    if (btn.board == selectedBoard.Name)
                    {
                        btn.board = null;
                        btn.UIBtn.Background = Brushes.LightGray;
                    }
                }

                // ItemsSource aktualisieren
                listBoards.ItemsSource = null;
                listBoards.ItemsSource = boardEintraege;
            }
            else
            {
                MessageBox.Show("Bitte ein Board auswählen.");
            }
        }

        private void zuweisen_Board(object sender, RoutedEventArgs e)
        {
            if (listBoards.SelectedItem == null)
            {
                MessageBox.Show("Bitte zuerst ein Board auswählen.");
                return;
            }

            var selectedBoard = listBoards.SelectedItem as BoardEntry;
            string board = selectedBoard?.Name.ToString();
            Brush boardFarbe = selectedBoard?.Farbe;

            foreach (var uiButton in ausgewaehlteButtons)
            {

                // Finde das zugehörige Datenobjekt in buttonsList
                var datenButton = Buttons.buttonsList.FirstOrDefault(b => b.UIBtn == uiButton);
                if (datenButton != null)
                {
                    datenButton.board = board;
                    datenButton.UIBtn.Background = boardFarbe;
                }
            }

            MessageBox.Show("Board wurde den ausgewählten Buttons zugewiesen.");
            reset_Buttons();
        }

        private void zuweisen_Size(object sender, RoutedEventArgs e)
        {
            if (schachtSize.Text == null)
            {
                MessageBox.Show("Bitte zuerst eine Größe angeben");
                return;
            }

            int size = 0;

            if (int.TryParse(schachtSize.Text, out size))
            {
                MessageBox.Show($"Größe: {size}");

            }
            else
            {
                MessageBox.Show("Bitte eien gültige Zahl eingeben.");
            }

            schachtSize.Items.Add(size);

            foreach (var uiButton in ausgewaehlteButtons)
            {

                // Finde das zugehörige Datenobjekt in buttonsList
                var datenButton = Buttons.buttonsList.FirstOrDefault(b => b.UIBtn == uiButton);
                if (datenButton != null)
                {
                    datenButton.size = size;
                    uiButton.Content = $"[{datenButton.spalte - 1},{datenButton.zeile - 1}] ({size})";
                    // 🔽 Hintergrundfarbe aktualisieren basierend auf dem zugewiesenen Board
                    if (!string.IsNullOrEmpty(datenButton.board))
                    {
                        var boardEintrag = boardEintraege.FirstOrDefault(b => b.Name == datenButton.board);
                        if (boardEintrag != null)
                        {
                            uiButton.Background = boardEintrag.Farbe;
                        }
                        else
                        {
                            uiButton.Background = Brushes.LightGray;
                        }
                    }
                    else
                    {
                        uiButton.Background = Brushes.LightGray;
                    }
                }
            }
            MessageBox.Show("Size wurde den ausgewählten Buttons zugewiesen.");
            reset_Buttons();
        }

        private void reset_Buttons()
        {
            //foreach(var btn in ausgewaehlteButtons)
            //{
            //    btn.Background = Brushes.LightGray;
            //}
            ausgewaehlteButtons.Clear();
            listAusgewaehlteButtons.Items.Clear();
        }

        private void Add_LockerSize(string Description, int Width, int Height)
        {
            UpdateConnectionString();
            string connectionString = _SQL_connWarehouse;
            string insertquery = @" IF NOT EXISTS (SELECT 1 FROM CompartmentSizes  WHERE Description = @Description AND AssociatedStore = @AssociatedStore)
            BEGIN
                INSERT INTO CompartmentSizes  (Description, Width, Height, Depth, AssociatedStore, CapacityCoefficient)
                VALUES  (@Description, @Width, @Height, @Depth, @AssociatedStore, @CapacityCoefficient)
            END";

            string filePath = @"C:\soft\configuration.sql";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(insertquery, connection))
                {
                    command.Parameters.AddWithValue("@Description", Description);
                    command.Parameters.AddWithValue("@Width", Width);
                    command.Parameters.AddWithValue("@Height", Height);
                    command.Parameters.AddWithValue("@Depth", 545);
                    command.Parameters.AddWithValue("@AssociatedStore", 2);
                    command.Parameters.AddWithValue("@CapacityCoefficient", 1);

                    command.ExecuteNonQuery();
                    command.Parameters.Clear();


                }
            }

            string insertSql = $@"IF NOT EXISTS (SELECT 1 FROM CompartmentSizes WHERE Description = '{EscapeSql(Description)}' AND AssociatedStore = 2)
                BEGIN
                    INSERT INTO CompartmentSizes (Description, Width, Height, Depth, AssociatedStore, CapacityCoefficient)
                    VALUES ('{EscapeSql(Description)}', {Width}, {Height}, 545, 2, 1)
                END
            ";

            File.AppendAllText(filePath, insertSql);
        }
        private void Locker_to_DB(object sender, RoutedEventArgs e)
        {
            if (comboSchrankvariante.SelectedItem == null)
            {
                MessageBox.Show("Konfiguriere zuerst deinen Locker");
                return;
            }

            add_Model("Locker", 2, int.Parse(positionX2.Text.ToString()), int.Parse(positionY2.Text.ToString()));

            UpdateConnectionString();
            string connectionString = _SQL_connWarehouse;
            string insertquery = "INSERT INTO Compartments (SizeId, PosX, PosY, PosZ, PosCode, Status, ReservationLimit, StoreId, AddressData) values (@SizeId, @PosX, @PosY, @PosZ, @PosCode, @Status, @ReservationLimit, @StoreId, @AddressData)";

            int sizeId = 0;
            string posCode = "";
            int storeId = get_StoreId();
            string Art = comboSchrankvariante.SelectedItem.ToString();
            string description = "";
            string address = "";
            int anzahl = 1;


            string filePath = @"C:\soft\configuration.sql";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                
                foreach (var btn in Buttons.buttonsList)
                {
                    if (Art == "A")
                    {
                        description = $"A{btn.size}";
                        Add_LockerSize(description, 180, btn.size);
                        sizeId = get_SizeId(description);
                    }
                    else if (Art == "B")
                    {
                        description = $"B{btn.size}";
                        Add_LockerSize(description, 300, btn.size);
                        sizeId = get_SizeId(description);
                    }
                    else if (Art == "C")
                    {
                        description = $"C{btn.size}";
                        Add_LockerSize(description, 450, btn.size);
                        sizeId = get_SizeId(description);
                    }
                    address = $"{{\"Port\":\"{btn.board}\",\"Door\":\"{anzahl:D2}\"}}";

                    using (SqlCommand command = new SqlCommand(insertquery, connection))
                    {
                        posCode = $"STL{storeId - 1}-{btn.zeile:D2}-{btn.spalte:D2}";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@SizeId", sizeId);
                        command.Parameters.AddWithValue("@PosX", btn.spalte);
                        command.Parameters.AddWithValue("@PosY", btn.zeile);
                        command.Parameters.AddWithValue("@PosZ", 0);
                        command.Parameters.AddWithValue("@PosCode", posCode);
                        command.Parameters.AddWithValue("@Status", 0);
                        command.Parameters.AddWithValue("@ReservationLimit", 1);
                        command.Parameters.AddWithValue("@StoreId", storeId);
                        command.Parameters.AddWithValue("@AddressData", address);

                        command.ExecuteNonQuery();
                    }
                    string sqlText = $@"INSERT INTO Compartments (SizeId, PosX, PosY, PosZ, PosCode, Status, ReservationLimit, StoreId, AddressData) VALUES ({sizeId}, {btn.spalte}, {btn.zeile}, 0, '{EscapeSql(posCode)}', 0, 1, {storeId}, '{EscapeSql(address)}');";

                    // SQL-Befehl ans Ende der Datei anhängen
                    File.AppendAllText(filePath, sqlText + Environment.NewLine);
                    anzahl++;
                }
            }
            MessageBox.Show("Locker in DB eingefügt");
            getStores();
        }

        Dictionary<int, int> listComNr = new Dictionary<int, int>();
        private void OldLocker_to_DB(object sender, RoutedEventArgs e)
        {
            if (comboSchrankvariante.SelectedItem == null)
            {
                MessageBox.Show("Konfiguriere zuerst deinen Locker");
                return;
            }
            createTable();
            OldModel();

            UpdateConnectionString();
            string connectionString = _SQL_connStoreManager;
            string insertquery = "INSERT INTO STL (Locker, x, y, Door, Board, size, ip, port, Doorname) values (@Locker, @x, @y, @Door, @Board, @size, @ip, @port, @Doorname)";

            string Art = comboSchrankvariante.SelectedItem.ToString();
            string description = "";
            int anzahl = 1;
            string doorname = "";
            string numberString = "";
            int comNr = 0;
            string ip = "COM";
            int door = 0;
            int board = 0;
            int letztePort = 0;
            string filePath = @"C:\soft\configuration.sql";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();


                foreach (var btn in Buttons.buttonsList)
                {
                    if (Art == "A")
                    {
                        description = $"A{btn.size}";

                    }
                    else if (Art == "B")
                    {
                        description = $"B{btn.size}";
                    }
                    else if (Art == "C")
                    {
                        description = $"C{btn.size}";
                    }
                    doorname = $"{anzahl}";
                    numberString = Regex.Match(btn.board, @"\d+").Value;
                    comNr = int.Parse(numberString);

                    if(letztePort != comNr)
                    {  
                        if (!listComNr.ContainsKey(comNr))
                        {
                            listComNr.Add(comNr, 0);
                        }
                        letztePort = comNr;
                        door = listComNr[comNr];
                        board++;
                    }

                    using (SqlCommand command = new SqlCommand(insertquery, connection))
                    {
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@Locker", Oldget_StoreId());
                        command.Parameters.AddWithValue("@x", btn.spalte);
                        command.Parameters.AddWithValue("@y", btn.zeile);
                        command.Parameters.AddWithValue("@Door", door);
                        command.Parameters.AddWithValue("@Board", board);
                        command.Parameters.AddWithValue("@size", description);
                        command.Parameters.AddWithValue("@ip", ip);
                        command.Parameters.AddWithValue("@port", comNr);
                        command.Parameters.AddWithValue("@Doorname", doorname);

                        command.ExecuteNonQuery();
                    }
                    string sqlText = $@"INSERT INTO STL (Locker, x, y, Door, Board, size, ip, port, Doorname) VALUES ({Oldget_StoreId()}, {btn.spalte}, {btn.zeile}, {door}, {board}, '{description}', {ip}, {comNr}, '{doorname}');";

                    // SQL-Befehl ans Ende der Datei anhängen
                    File.AppendAllText(filePath, sqlText + Environment.NewLine);
                    anzahl++;
                    door++;
                    listComNr[comNr] = door;
                }
            }
        }

        private void OldModel()
        {
            UpdateConnectionString();
            string connectionString = _SQL_connStoreManager;
            string insertquery = "INSERT INTO SerialPorts(AutomatNr, Port, Position, Schranktyp) values(@AutomatNr, @Port, @Position, @Schranktyp)";

            int automatNr = Oldget_StoreId() + 1;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    int inserted = 0;
                    connection.Open();


                    using (SqlCommand command = new SqlCommand(insertquery, connection))
                    {
                        command.Parameters.AddWithValue("@AutomatNr", automatNr);
                        command.Parameters.AddWithValue("@Port", "STL");
                        command.Parameters.AddWithValue("@Position", int.Parse(positionX2.Text.ToString()));
                        command.Parameters.AddWithValue("@Schranktyp", 2);
                        inserted += command.ExecuteNonQuery();
                    }
                }

                string sqlInsert = $"INSERT INTO SerialPorts(AutomatNr, Port, Position, Schranktyp) values ('{automatNr}','STL', {int.Parse(positionX2.Text.ToString())},2);{Environment.NewLine}";

                // Pfad zur SQL-Datei (kann angepasst werden)
                string filePath = @"C:\soft\configuration.sql";

                // In Datei anhängen
                File.AppendAllText(filePath, sqlInsert);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Einfügen des Stores");
            }
        }

        private int Oldget_StoreId()
        {
            UpdateConnectionString();
            string connectionString = _SQL_connStoreManager;
            string query = "Select top(1) AutomatNr  from SerialPorts order by AutomatNr desc";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        reader.Read();
                        return (Byte)reader["AutomatNr"];
                    }
                }
            }
        }

        private void createTable()
        {
            UpdateConnectionString();
            string connectionString = _SQL_connStoreManager;
            string createTable = @"
        IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'STL')
        BEGIN
            CREATE TABLE [dbo].[STL](
                [id] [int] IDENTITY(1,1) NOT NULL,
                [Locker] [int] NULL,
                [x] [int] NULL,
                [y] [int] NULL,
                [Door] [int] NULL,
                [Board] [int] NULL,
                [size] [nvarchar](10) NULL,
	            [ip] [nvarchar](20) NULL,
                [port] [int] NULL,
                [ArtikelID] [int] NULL,
                [Doorname] [varchar](max) NULL,
                CONSTRAINT [PK_STL] PRIMARY KEY CLUSTERED 
                (
                    [id] ASC
                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, 
                       IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, 
                       ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF
                ) ON [PRIMARY]
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
            END";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(createTable, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private Dictionary<string, Brush> boardColors = new Dictionary<string, Brush>();

        private List<Brush> verfügbareFarben = new List<Brush>
        {
            Brushes.LightBlue, Brushes.Green, Brushes.Orange,
            Brushes.Purple, Brushes.CadetBlue, Brushes.Teal, Brushes.Salmon,
            Brushes.Olive, Brushes.Crimson, Brushes.DarkCyan, Brushes.Red
        };

        public class BoardEntry
        {
            public string Name { get; set; }
            public Brush Farbe { get; set; }
        }

        private List<BoardEntry> boardEintraege = new List<BoardEntry>();


        private Point selectionStart;
        private bool isSelecting = false;

        private void selectionCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            selectionStart = e.GetPosition(selectionCanvas);
            isSelecting = true;

            Canvas.SetLeft(selectionRectangle, selectionStart.X);
            Canvas.SetTop(selectionRectangle, selectionStart.Y);
            selectionRectangle.Width = 0;
            selectionRectangle.Height = 0;
            selectionRectangle.Visibility = Visibility.Visible;

            selectionCanvas.CaptureMouse();
        }

        private void selectionCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isSelecting) return;

            Point current = e.GetPosition(selectionCanvas);

            double x = Math.Min(current.X, selectionStart.X);
            double y = Math.Min(current.Y, selectionStart.Y);
            double width = Math.Abs(current.X - selectionStart.X);
            double height = Math.Abs(current.Y - selectionStart.Y);

            Canvas.SetLeft(selectionRectangle, x);
            Canvas.SetTop(selectionRectangle, y);
            selectionRectangle.Width = width;
            selectionRectangle.Height = height;
        }

        private void selectionCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!isSelecting) return;

            isSelecting = false;
            selectionRectangle.Visibility = Visibility.Collapsed;
            selectionCanvas.ReleaseMouseCapture();

            Rect selectionArea = new Rect(Canvas.GetLeft(selectionRectangle),
                                          Canvas.GetTop(selectionRectangle),
                                          selectionRectangle.Width,
                                          selectionRectangle.Height);

            // Alle Buttons im Rechteck sammeln
            var getroffeneButtons = matrixGrid.Children
                .OfType<Button>()
                .Where(btn =>
                {
                    Point btnPos = btn.TransformToAncestor(selectionCanvas).Transform(new Point(0, 0));
                    Rect btnBounds = new Rect(btnPos, new Size(btn.ActualWidth, btn.ActualHeight));
                    return selectionArea.IntersectsWith(btnBounds);
                })
                .ToList();

            // Prüfen ob alle bereits ausgewählt sind
            bool alleBereitsAusgewaehlt = getroffeneButtons.All(btn => ausgewaehlteButtons.Contains(btn));

            foreach (var btn in getroffeneButtons)
            {
                if (alleBereitsAusgewaehlt)
                {
                    // Entmarkieren
                    ausgewaehlteButtons.Remove(btn);

                    var datenButton = Buttons.buttonsList.FirstOrDefault(b => b.UIBtn == btn);
                    if (datenButton != null && !string.IsNullOrEmpty(datenButton.board))
                    {
                        var boardEintrag = boardEintraege.FirstOrDefault(b => b.Name == datenButton.board);
                        btn.Background = boardEintrag?.Farbe ?? Brushes.LightGray;
                    }
                    else
                    {
                        btn.Background = Brushes.LightGray;
                    }
                }
                else
                {
                    // Neu markieren, falls noch nicht markiert
                    if (!ausgewaehlteButtons.Contains(btn))
                    {
                        ausgewaehlteButtons.Add(btn);
                        btn.Background = Brushes.Blue;
                    }
                }
            }

            // Aktualisiere die Liste
            listAusgewaehlteButtons.Items.Clear();
            string content = string.Join(" ; ", ausgewaehlteButtons.Select(b => b.Content.ToString()));
            listAusgewaehlteButtons.Items.Add(content);
        }

        private string EscapeSql(string input)
        {
            if (input == null)
                return null;
            return input.Replace("'", "''");
        }

    }
}
