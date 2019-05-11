using Microsoft.EntityFrameworkCore;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore.Internal;

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
        public Person Person { get; set; }

        public ICollection<FoundGeocache> FoundGeocaches { get; set; }
    }

    public class FoundGeocache
    {

        public int PersonID { get; set; }
        public Person Person { get; set; }
        public int GeoCacheID { get; set; }
        public Geocache Geocache { get; set; }
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

        private AppDbContext database = new AppDbContext();
        private MapLayer layer;

        private Location location;
        private Location latestClickLocation;
        private Location gothenburg = new Location(57.719021, 11.991202);
        private Person SelectedPerson = null;


        // Contains the location of the latest click on the map.
        // The Location object in turn contains information like longitude and latitude.

        public MainWindow()
        {
            InitializeComponent();
            Start();
        }

        private void Start()
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            if (applicationId == null)
            {
                MessageBox.Show("Please set the applicationId variable before running this program.");
                Environment.Exit(0);
            }

            CreateMap();

            using (var db = new AppDbContext())
            {
                // Load data from database and populate map here.
            }
        }

        private void CreateMap()
        {
            map.CredentialsProvider = new ApplicationIdCredentialsProvider(applicationId);
            map.Center = gothenburg;
            map.ZoomLevel = 12;
            layer = new MapLayer();
            map.Children.Add(layer);

            map.MouseDown += (sender, e) =>
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


            //Ladda in personer och geocaches..
            LoadPersonFromDataBase();
            LoadGeocacheFromDataBase();
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
            SelectedPerson = null;

            foreach (UIElement element in layer.Children)
            {
                if (element is Pushpin)
                {
                    element.Opacity = 1.0;

                    var pin = (Pushpin)element;

                    if (pin.Tag is Geocache)
                    {
                        pin.Background = new SolidColorBrush(Colors.Gray);
                    }
                }
            }
        }



        //färdig
        private void OnAddPersonClick(object sender, RoutedEventArgs args)
        {
            var dialog = new PersonDialog();
            dialog.Owner = this;
            dialog.ShowDialog();
            if (dialog.DialogResult == false)
            {
                return;
            }
            //skapar ett person objekt.
            var person = new Person
            {
                FirstName = dialog.PersonFirstName,
                LastName = dialog.PersonLastName,
                Country = dialog.AddressCountry,
                City = dialog.AddressCity,
                StreetName = dialog.AddressStreetName,
                StreetNumber = dialog.AddressStreetNumber,
            };
            database.Add(person);
            database.SaveChanges();

            // Add person to map and database here.
            //FIXA TOOLTIPPEN HÄR
            var pin = AddPin(latestClickLocation, person.FirstName + " " + person.LastName + "\n" + person.Country + " "
                                          + person.City + " " + person.StreetName + " " + person.StreetNumber, Colors.Blue, 1, person);

            SelectedPerson = person;

            pin.MouseDown += (e, a) =>
            {
                //Anropa metod vid knapptryck
                CurrentUserPin(pin, person);

                UpdateMap();

                // Prevent click from being triggered on map.
                a.Handled = true;
            };
        }

        private void OnAddGeocacheClick(object sender, RoutedEventArgs args)
        {
            if (SelectedPerson != null)
            {
                var dialog = new GeocacheDialog();
                dialog.Owner = this;
                dialog.ShowDialog();

                if (dialog.DialogResult == false)
                {
                    return;
                }

                Geocache geocache = new Geocache
                {
                    PersonID = SelectedPerson.ID,
                    Latitude = latestClickLocation.Latitude,
                    Longitude = latestClickLocation.Longitude,
                    Contents = dialog.GeocacheContents,
                    Message = dialog.GeocacheMessage
                };
                database.Add(geocache);
                database.SaveChanges();

                //skapa en location för geocachen.
                Location location = new Location
                {
                    Latitude = geocache.Latitude,
                    Longitude = geocache.Longitude,
                };

                var addGeoPin = AddPin(location, geocache.Message, Colors.Gray, 1, geocache);
            }

            // Add geocache to map and database here.
            var pin = AddPin(latestClickLocation, "Person", Colors.Gray, 1, "Person");

            pin.MouseDown += (s, a) =>
            {
                // Handle click on geocache pin here.
                MessageBox.Show("You clicked a geocache");
                UpdateMap();

                // Prevent click from being triggered on map.
                a.Handled = true;
            };
        }

        //ladda in personer från databasen. - FÄRDIG!
        private void LoadPersonFromDataBase()
        {
            //include added!
            var personPin = database.Person.Include(p => p.FoundGeocaches);


            foreach (Person person in personPin)
            {
                Location location = new Location();
                location.Latitude = person.Latitude;
                location.Longitude = person.Longitude;

                var pin = AddPin(location, person.FirstName + "" + person.LastName + "\n" + person.Country + "" + person.City + "\n"
                                                + person.StreetName + person.StreetNumber, Colors.Blue, 1, person);

                pin.MouseDown += (c, a) =>
                {
                    //Anropa metod vid knapptryck
                    CurrentUserPin(pin, person);

                    a.Handled = true;
                };
            }
        }

        //ladda in geocaches från databasen.
        private void LoadGeocacheFromDataBase()
        {
            var geocachePin = database.Geocache.Include(g => g.Person);

            foreach (Geocache geocache in geocachePin)
            {
                Location location = new Location();
                location.Latitude = geocache.Latitude;
                location.Longitude = geocache.Longitude;

                var pin = AddPin(location, geocache.Message, Colors.Gray, 1, geocache);


                pin.MouseDown += SelectedGeo;

            }
        }

        //utökar metoden med opacity och object parameter.
        private Pushpin AddPin(Location location, string tooltip, Color color, double opacity, object tag)
        {
            var pin = new Pushpin();
            pin.Cursor = Cursors.Hand;
            pin.Opacity = opacity;
            pin.Tag = tag;
            pin.Background = new SolidColorBrush(color);
            ToolTipService.SetToolTip(pin, tooltip);
            ToolTipService.SetInitialShowDelay(pin, 0);
            layer.AddChild(pin, new Location(location.Latitude, location.Longitude));
            return pin;

        }

        private void CurrentUserPin(Pushpin pin, Person person)
        {
            SelectedPerson = person;
            pin.Opacity = 1;

            //loopa igenom alla element.
            foreach (UIElement allElements in layer.Children)
            {
                if (allElements is Pushpin)
                {
                    var newPin = (Pushpin)allElements;

                    if (newPin.Tag is Person)
                    {
                        //sänk opacity om det inte är samma user
                        if (pin != newPin)
                        {
                            newPin.Opacity = 0.5;
                        }
                    }
                    else if (newPin.Tag is Geocache)
                    {
                        var geoPin = (Geocache)newPin.Tag;

                        if (geoPin.Person == person)
                        {
                            newPin.Background = new SolidColorBrush(Colors.Black);
                        }
                        else if (person.FoundGeocaches != null && person.FoundGeocaches.Any(fg => fg.GeoCacheID == geoPin.ID))
                        {
                            newPin.Background = new SolidColorBrush(Colors.Green);
                        }

                        else
                        {
                            newPin.Background = new SolidColorBrush(Colors.Red);
                        }
                    }
                }
            }
        }

        private void UpdateColor(Pushpin pin, Color color, double opacity)
        {
            pin.Opacity = opacity;
            pin.Background = new SolidColorBrush(color);
        }

        private void SelectedGeo(object sender, MouseButtonEventArgs e)
        {
            Pushpin pin = (Pushpin)sender;
            Geocache geocache = (Geocache)pin.Tag;

            Brush redBrush = new SolidColorBrush(Colors.Red);
            Brush greenBrush = new SolidColorBrush(Colors.Green);

            if (SelectedPerson == null)
            {
                MessageBox.Show("Please select a person first");

                e.Handled = true;
            }

            if (pin.Background.ToString() != redBrush.ToString())
            {
                try
                {
                    FoundGeocache foundgeo = database.FoundGeocache.FirstOrDefault(fg =>
                        fg.PersonID == SelectedPerson.ID && fg.GeoCacheID == geocache.ID);

                    database.Remove(foundgeo);
                    database.SaveChanges();

                    UpdateColor(pin, Colors.Red, 1);



                    e.Handled = true;
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }
            }
            else if (pin.Background.ToString() != greenBrush.ToString())
            {
                FoundGeocache foundgeoche = new FoundGeocache
                {
                    Person = SelectedPerson,
                    Geocache = geocache,
                };
                try
                {
                    database.Add(foundgeoche);
                    database.SaveChanges();

                    UpdateColor(pin, Colors.Green, 1);
                    e.Handled = true;

                }
                catch (Exception ex)
                {
                    ex.ToString();
                }
            }

        }

        //färdig
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

            // A 2d List that holds other lists of the type "string". 

            List<List<String>> collection = new List<List<string>>();
            List<string> linesWithObjects = new List<string>();

            //A list that holds Persons and a string list that holds their found geocaches.
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
                            Geocache = geoCaches[int.Parse(geoS) - 1]
                        };
                        database.Add(userNewGeo);
                        database.SaveChanges();
                    }
                }
            }
        }

        //Färdig
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

            //En lista som håller informationen vi Queryar fram.
            List<string> containerList = new List<string>();

            //För varje person i databasen vill vi först skriva ut personobjektet...
            foreach (var p in database.Person)
            {
                containerList.Add($"{p.FirstName} | {p.LastName} | {p.Country} | {p.City} | {p.StreetName} | {p.StreetNumber} | {p.Latitude} | {p.Longitude}");

                //Vi vill även ha det objektets egna Geocaches som har lagts ut..
                foreach (var geo in database.Geocache.Where(g => g.PersonID == p.ID))
                {
                    containerList.Add($"{geo.ID} | {geo.Latitude} | {geo.Longitude} | {geo.Contents} | {geo.Message}");
                }

                //Och här vill vi loopa fram de geocaches som personen har hittat. Vi loopar igenom FoundGeocaches och för varje element(uppfunnen geocache) vi hittar 
                //vill vi lägga till den uppfunna geocachensID. geoIDS innehåller alltså dom ID på dom uppfunna geocaches. 

                FoundGeocache[] foundCaches = database.FoundGeocache.Where(fg => fg.PersonID == p.ID).OrderByDescending(o => o).ToArray();

                string geoIDs = "";

                for (int i = 0; i < foundCaches.Length; i++)
                {
                    geoIDs += foundCaches[i].GeoCacheID;

                    if (i < foundCaches.Length - 1)
                    {
                        geoIDs += ", ";
                    }
                }
                containerList.Add("Found: " + geoIDs);
                containerList.Add("");
            }

            File.WriteAllLines(path, containerList);
        }
    }
}

