using Microsoft.EntityFrameworkCore;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;

namespace Geocaching
{
    public class Person
    {

        public int ID { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string StreetName { get; set; }
        public int StreetNumber { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public ICollection<Geocache> Geocaches { get; set; }

        public ICollection<FoundGeocache> FoundGeocaches { get; set; }
    }

    public class Geocache
    {

        public int ID { get; set; }

        public int? PersonID { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Contents { get; set; }
        public string Message { get; set; }
        public Person Person  { get; set; }

        public ICollection<FoundGeocache> FoundGeocaches { get; set; }

    }

    public class FoundGeocache
    {

        public int PersonID { get; set; }
        public Person Person { get; set; }
        public int GeoCacheID { get; set; }
        public  Geocache Geocache { get; set; }


    }

    class AppDbContext : DbContext
    {
        public DbSet<Person> Person { get; set; }
        public DbSet<Geocache> Geocache { get; set; }
        public DbSet<FoundGeocache> FoundGeocache { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(@"Data Source=(local)\SQLEXPRESS;Initial Catalog=Geocaching;Integrated Security=True");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //The person class attributes and properties.

            modelBuilder.Entity<Person>(ep => { ep.Property(p => p.FirstName).HasColumnType("nvarchar(50)"); });
            modelBuilder.Entity<Person>(ep => { ep.Property(p => p.LastName).HasColumnType("nvarchar(50)"); });
            modelBuilder.Entity<Person>(ep => { ep.Property(p => p.Latitude).HasColumnType("float"); });
            modelBuilder.Entity<Person>(ep => { ep.Property(p => p.Longitude).HasColumnType("float"); });
            modelBuilder.Entity<Person>(ep => { ep.Property(p => p.Country).HasColumnType("nvarchar(50)"); });
            modelBuilder.Entity<Person>(ep => { ep.Property(p => p.City).HasColumnType("nvarchar(50)"); });
            modelBuilder.Entity<Person>(ep => { ep.Property(p => p.StreetName).HasColumnType("nvarchar(50)"); });
            modelBuilder.Entity<Person>(ep => { ep.Property(p => p.StreetNumber).HasColumnType("tinyint"); });


            //Here is the geocache class attributes and properties.

            modelBuilder.Entity<Geocache>(eg => { eg.Property(g => g.ID).HasColumnType("int"); });
            modelBuilder.Entity<Geocache>(eg => { eg.Property(g => g.PersonID).HasColumnType("int"); });
            modelBuilder.Entity<Geocache>(eg => { eg.Property(g => g.Latitude).HasColumnType("float"); });
            modelBuilder.Entity<Geocache>(eg => { eg.Property(g => g.Longitude).HasColumnType("float"); });
            modelBuilder.Entity<Geocache>(eg => { eg.Property(g => g.Contents).HasColumnType("nvarchar(255)").IsRequired(); });
            modelBuilder.Entity<Geocache>(eg => { eg.Property(g => g.Message).HasColumnType("nvarchar(255)").IsRequired(); });

            //Many to many tabellen där kolumnerna PersonID och GeoCacheId har foreign keys.

            modelBuilder.Entity<FoundGeocache>()
                .HasKey(fg => new { fg.PersonID, fg.GeoCacheID });

        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Contains the ID string needed to use the Bing map.
        // Instructions here: https://docs.microsoft.com/en-us/bingmaps/getting-started/bing-maps-dev-center-help/getting-a-bing-maps-key

        private const string applicationId = "AgiqDKrnFmQ3B6tTb3XHMXzuUY8hrVhlrsffqfaNnEmeQmLLz2me8wJ_D2Q744Md";


        //Detta är databasvariabeln vi kallar på för att spara i själva Databasen.
       private AppDbContext database = new AppDbContext();

        private MapLayer layer;

        // Contains the location of the latest click on the map.
        // The Location object in turn contains information like longitude and latitude.
        private Location latestClickLocation;

        private Location gothenburg = new Location(57.719021, 11.991202);

        public MainWindow()
        {
            InitializeComponent();
            Start();
        }

        private void Start()
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;


            CreateMap();

        }

        private void CreateMap()
        {
            map.CredentialsProvider = new ApplicationIdCredentialsProvider(applicationId);
            map.Center = gothenburg;
            map.ZoomLevel = 12;
            layer = new MapLayer();
            map.Children.Add(layer);

            MouseDown += (sender, e) =>
            {
                var point = e.GetPosition(this);
                latestClickLocation = map.ViewportPointToLocation(point);

                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    OnMapLeftClick();
                }
            };

            map.ContextMenu = new ContextMenu();

            var addPersonMenuItem = new MenuItem { Header = "Add Person" };
            map.ContextMenu.Items.Add(addPersonMenuItem);
            addPersonMenuItem.Click += OnAddPersonClick;

            var addGeocacheMenuItem = new MenuItem { Header = "Add Geocache" };
            map.ContextMenu.Items.Add(addGeocacheMenuItem);
            addGeocacheMenuItem.Click += OnAddGeocacheClick;
        }

        private void UpdateMap()
        {
            // It is recommended (but optional) to use this method for setting the color and opacity of each pin after every user interaction that might change something.
            // This method should then be called once after every significant action, such as clicking on a pin, clicking on the map, or clicking a context menu option.
        }

        private void OnMapLeftClick()
        {
            // Handle map click here.
            UpdateMap();
        }

        private void OnAddGeocacheClick(object sender, RoutedEventArgs args)
        {
            var dialog = new GeocacheDialog();
            dialog.Owner = this;
            dialog.ShowDialog();
            if (dialog.DialogResult == false)
            {
                return;
            }

            string contents = dialog.GeocacheContents;
            string message = dialog.GeocacheMessage;
            // Add geocache to map and database here.

            Geocache geocache = new Geocache();
            geocache.Contents = dialog.GeocacheContents;
            geocache.Message = dialog.GeocacheMessage;

            database.Add(geocache);
            database.SaveChanges();

            var pin = AddPin(latestClickLocation, "Person", Colors.Gray);

            pin.MouseDown += (s, a) =>
            {
                // Handle click on geocache pin here.
                MessageBox.Show("You clicked a geocache");
                UpdateMap();

                // Prevent click from being triggered on map.
                a.Handled = true;
            };
        }

        private void OnAddPersonClick(object sender, RoutedEventArgs args)
        {
            var dialog = new PersonDialog();
            dialog.Owner = this;
            dialog.ShowDialog();
            if (dialog.DialogResult == false)
            {
                return;
            }

            string city = dialog.AddressCity;
            string country = dialog.AddressCountry;
            string streetName = dialog.AddressStreetName;
            int streetNumber = dialog.AddressStreetNumber;
           
            // Person here is added to map and the database. 

            Person person = new Person();
            person.FirstName = dialog.PersonFirstName;
            person.LastName = dialog.PersonLastName;
            person.Country = dialog.AddressCountry;
            person.City = dialog.AddressCity;
            person.StreetName = dialog.AddressStreetName;
            person.StreetNumber = dialog.AddressStreetNumber;

            database.Add(person);
            database.SaveChanges();

            var pin = AddPin(latestClickLocation, "Person", Colors.Blue);

            pin.MouseDown += (s, a) =>
            {
                // Handle click on person pin here.
                MessageBox.Show("You clicked a person");
                UpdateMap();

                // Prevent click from being triggered on map.
                a.Handled = true;
            };
        }

        private Pushpin AddPin(Location location, string tooltip, Color color)
        {
            var pin = new Pushpin();
            pin.Cursor = Cursors.Hand;
            pin.Background = new SolidColorBrush(color);
            ToolTipService.SetToolTip(pin, tooltip);
            ToolTipService.SetInitialShowDelay(pin, 0);
            layer.AddChild(pin, new Location(location.Latitude, location.Longitude));
            return pin;
        }

        private void OnLoadFromFileClick(object sender, RoutedEventArgs args)
        {
            Person userPerson = new Person();

            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";
            bool? result = dialog.ShowDialog();
            if (result != true)
            {
                return;
            }

            string path = dialog.FileName;
            // Read the selected file here.

            List<List<String>> collection = new List<List<string>>();
            List<string> linesWithObjects = new List<string>(); 

            //alternative testing
            List<Person> peopleList = new List<Person>();
            List<string> foundValues = new List<string>();

            database.Person.RemoveRange(database.Person);
            database.Geocache.RemoveRange(database.Geocache);
            database.FoundGeocache.RemoveRange(database.FoundGeocache);

            string[] lines = File.ReadAllLines(path).ToArray();

            foreach (var line in lines)
            {
                if (line != "")
                {
                    linesWithObjects.Add(line);
                    continue;
                }
                else
                {
                    collection.Add(linesWithObjects);
                    linesWithObjects = new List<string>();
                }
            }
            collection.Add(linesWithObjects);


            foreach (List<string> personLines in collection)
            {
                for (int i = 0; i < personLines.Count; i++)
                {
                    string[] values = personLines[i].Split('|').Select(v => v.Trim()).ToArray();

                    if (personLines[i].StartsWith("Found:"))
                    {
                        foundValues.Add(personLines[i]); 
                    }
                     
                    else if (values.Length > 5)
                    {

                       string FirstName = values[0];
                       string LastName = values[1];
                       string Country = values[2];
                       string City = values[3];
                       string StreetName = values[4];
                       int StreetNumber = int.Parse(values[5]);
                       double Latitude = double.Parse(values[6]);
                       double Longtitude = double.Parse(values[7]);

                        userPerson = new Person
                        {
                           FirstName = FirstName,
                           LastName = LastName,
                           Country = Country,
                           City = City,
                           StreetName = StreetName,
                           StreetNumber = StreetNumber,
                           Latitude = Latitude,
                           Longitude = Longtitude,
                        };
                         peopleList.Add(userPerson);
                         database.Add(userPerson);
                         database.SaveChanges();
                       
                    }

                    else if (values.Length == 5)
                    {
                        int personId = int.Parse(values[0]);
                        double latitude = double.Parse(values[1]);
                        double longitude = double.Parse(values[2]);
                        string contents = values[3];
                        string message = values[4];

                        Geocache userGeocache = new Geocache
                        {
                            Latitude = latitude,
                            Longitude = longitude,
                            Contents = contents,
                            Message = message,
                        };
                        userGeocache.Person = userPerson;
                        database.Add(userGeocache);
                        database.SaveChanges();
                    }
                }
            }
            if (foundValues[0].StartsWith("Found:"))
            {
                for (int i = 0; i < foundValues.Count; i++)
                {
                    foundValues[i] = foundValues[i].Trim("Found: ".ToCharArray());
                    foundValues[i] = foundValues[i].Trim(" ".ToCharArray());
                    var indexes = foundValues[i].Split(',').ToArray();
                    var geoCaches = database.Geocache.ToList();

                    foreach (var geoS in indexes)
                    {
                        FoundGeocache userNewGeo = new FoundGeocache
                        {
                            Person = peopleList[i],
                            Geocache = geoCaches[int.Parse(geoS) -1]
                        };
                        database.Add(userNewGeo);
                        database.SaveChanges();
                    }
                }
            }
        }

        private void OnSaveToFileClick(object sender, RoutedEventArgs args)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";
            dialog.FileName = "Geocaches";
            bool? result = dialog.ShowDialog();
            if (result != true)
            {
                return;
            }

            string path = dialog.FileName;
            // Write to the selected file here.

            var personsFromDataBase = database.Person.FromSql("SELECT FirstName, LastName, Country, City, StreetName, StreetNumber, Latitude, Longitude");

            var personsCaught = personsFromDataBase.ToString().Split('|').Select(p => p.Trim()).ToArray();

            foreach (var persons in personsCaught)
            {

            }
            //for (int j = 0; j < indexes.Length; j++)
            //{
            //    FoundGeocache userFoundGeocache = new FoundGeocache
            //    {
            //        Person = peopleList[i],
            //        Geocache = geoCaches[int.Parse(indexes[j])]
            //    };
            //    database.Add(userFoundGeocache);
            //    database.SaveChanges();
            //};


        }
    }
}

