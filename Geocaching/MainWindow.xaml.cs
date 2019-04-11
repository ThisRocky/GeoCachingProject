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
        public int ID { get; set; }
        [ForeignKey("PersonID")]
        public int PersonID { get; set; }
        public Person Person { get; set; }
        [ForeignKey("GeoCacheID")]
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
            options.UseSqlServer(@"Data Source=(local)\SQLEXPRESS;Initial Catalog=Geocache;Integrated Security=True");
        }
        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<FoundGeocache>()
                .HasOne(fg => fg.Person)
                .WithMany(p => p.FoundGeocaches)
                .HasForeignKey(fg => fg.PersonID);

            model.Entity<FoundGeocache>()
                .HasOne(fg => fg.Geocache)
                .WithMany(g => g.FoundGeocaches)
                .HasForeignKey(fg => fg.GeoCacheID);
        } }
    
 
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window

    {
        // Contains the ID string needed to use the Bing map.
        // Instructions here: https://docs.microsoft.com/en-us/bingmaps/getting-started/bing-maps-dev-center-help/getting-a-bing-maps-key

        private const string applicationId = "AgiqDKrnFmQ3B6tTb3XHMXzuUY8hrVhlrsffqfaNnEmeQmLLz2me8wJ_D2Q744Md";


        //Detta är databasvariabeln vi kallar på för att spara i själva Databasen.
        private AppDbContext database = new AppDbContext();

        private MapLayer layer;
        private Location latestClickLocation;
        private Location gothenburg = new Location(57.719021, 11.991202);
        private Location location;
        private Person selectedPerson = null;

        // Contains the location of the latest click on the map.
        // The Location object in turn contains information like longitude and latitude.
        Object lockThis = new Object();

        

        public MainWindow()
        {
            latestClickLocation = gothenburg;
            InitializeComponent();
            Start();
        }

        private void Start()
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;


            CreateMap();

        }

        private async void CreateMap()
        {
            try
            {
                layer.Children.Clear();
            }
            catch
            {

            }
            map.CredentialsProvider = new ApplicationIdCredentialsProvider(applicationId);
            map.Center = new Location { Latitude = gothenburg.Latitude, Longitude = gothenburg.Longitude };
            map.ZoomLevel = 12;
            layer = new MapLayer();
            map.Children.Add(layer);

            MouseDown += (sender, e) =>
            {
                var point = e.GetPosition(this); 
                latestClickLocation.Latitude = map.ViewportPointToLocation(point).Latitude;
                latestClickLocation.Longitude = map.ViewportPointToLocation(point).Longitude;

                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    selectedPerson = null;

                    foreach (Pushpin pin in layer.Children)
                    {
                        try
                        {
                            Geocache geocache = (Geocache)pin.Tag;
                            PinRefresh(pin, Colors.Gray, 1);
                        }
                        catch
                        {
                            Person person = (Person)pin.Tag;
                            PinRefresh(pin, Colors.Blue, 1);
                        }
                    }
                }
            };
            var geocaches = await Task.Run(() =>
            {
                return database.Geocache.Include(g => g.Person);
            });

            foreach (Geocache GeoCache in geocaches)
            {
                location = new Location();
                location.Latitude = GeoCache.Latitude;
                location.Longitude = GeoCache.Longitude;
                var pin = AddPin(location, GeoCache.Message, Colors.Gray, 1, GeoCache);
            }

            var personpins = await Task.Run(() =>
            {
                return database.Person.ToArray();
            });

            foreach (Person person in personpins)
            {
                location = new Location();
                location.Latitude = person.Latitude;
                location.Longitude = person.Longitude;
                var pin = AddPin(location, person.FirstName + " " + person.LastName + "\n" + person.StreetName + " " + person.StreetNumber + "\n" + person.City, Colors.Blue, 1, person);

                pin.MouseDown += SelectedPerson;
            }

            map.ContextMenu = new ContextMenu();

            var addPersonMenuItem = new MenuItem { Header = "Add Person" };
            map.ContextMenu.Items.Add(addPersonMenuItem);
            addPersonMenuItem.Click += OnAddPersonClick;

            var addGeocacheMenuItem = new MenuItem { Header = "Add Geocache" };
            map.ContextMenu.Items.Add(addGeocacheMenuItem);
            addGeocacheMenuItem.Click += OnAddGeocacheClickAsync;
        }
        private void PinRefresh(Pushpin pin, Color color, double opacity)
        {
            pin.Background = new SolidColorBrush(color);
            pin.Opacity = opacity;
        }
        private void UpdateMap()
        {
          // It is recommended (but optional) to use this method for setting the color and opacity of each pin after every user interaction that might change something.
           // This method should then be called once after every significant action, such as clicking on a pin, clicking on the map, or clicking a context menu option.
        }

      
        private void Handled(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
        private void SelectedPerson(object sender, MouseButtonEventArgs e)
        {
            Geocache[] geocaches = null;
            var Geocaches = Task.Run(() =>
            {
                geocaches = database.Geocache.Select(a => a).ToArray();
            });

            Pushpin pin = (Pushpin)sender;
            Person person = (Person)pin.Tag;
            string tooptipp = pin.ToolTip.ToString();
            selectedPerson = person;
            PinRefresh(pin, Colors.Blue, 1);

            Task.WaitAll(Geocaches);

            foreach (Pushpin p in layer.Children)
            {

                try { p.MouseDown -= Green; }
                catch { }
                try { p.MouseDown -= Red; }
                catch { }
                try { p.MouseDown -= Handled; }
                catch { }


                Geocache geocache = geocaches
                .FirstOrDefault(g => g.Latitude == p.Location.Latitude && g.Longitude == p.Location.Longitude);

                FoundGeocache foundGeocache = null;
                if (geocache != null)
                {
                    foundGeocache = database.FoundGeocache
                        .FirstOrDefault(fg => fg.GeoCacheID == geocache.ID && fg.PersonID == person.ID);
                }

                if (geocache == null && p.ToolTip.ToString() != tooptipp)
                {
                    PinRefresh(p, Colors.Blue, 0.5);
                }

                else if (geocache != null && geocache.PersonID == person.ID)
                {
                    PinRefresh(p, Colors.Black, 1);
                    p.MouseDown += Handled;
                }

                else if (geocache != null && foundGeocache != null)
                {
                    PinRefresh(p, Colors.Green, 1);
                    p.MouseDown += Green;
                }

                else if (geocache != null && foundGeocache == null)
                {
                    PinRefresh(p, Colors.Red, 1);
                    p.MouseDown += Red;
                }
                e.Handled = true;
            }
        }
        private void Green(object sender, MouseButtonEventArgs e)
        {

            Pushpin pin = (Pushpin)sender;
            Geocache geocache = (Geocache)pin.Tag;
            try
            {
                FoundGeocache foundGeocache = database.FoundGeocache
               .FirstOrDefault(fg => fg.PersonID == selectedPerson.ID && fg.GeoCacheID == geocache.ID);
                database.Remove(foundGeocache);
            }
            catch { }

            try { database.SaveChanges(); }
            catch { }
            PinRefresh(pin, Colors.Red, 1);
            pin.MouseDown -= Green;
            pin.MouseDown += Red;
            e.Handled = true;
        }

        private void Red(object sender, MouseButtonEventArgs e)
        {
            Pushpin pin = (Pushpin)sender;
            Geocache geocache = (Geocache)pin.Tag;
            FoundGeocache foundGeocache = new FoundGeocache
            {
                Person = selectedPerson,
                Geocache = geocache
            };
            database.Add(foundGeocache);
            try { database.SaveChanges(); }
            catch { }
            PinRefresh(pin, Colors.Green, 1);
            pin.MouseDown -= Red;
            pin.MouseDown += Green;
            e.Handled = true;
        }

    
        private void OnMapLeftClick()
        {
            // Handle map click here.
            UpdateMap();
        }

        private async void OnAddGeocacheClickAsync(object sender, RoutedEventArgs args)
        {
            if (selectedPerson != null)
            {
                var dialog = new GeocacheDialog();
                dialog.Owner = this;
                dialog.ShowDialog();

            if (dialog.DialogResult == false)
            {
                return;
            }

<<<<<<< HEAD
            //string contents = dialog.GeocacheContents;
            //string message = dialog.GeocacheMessage;
=======
           
>>>>>>> 760ea465a0aaa6bbe52a940613f54bfba37cedcb
            // Add geocache to map and database here.

            Geocache geocache = new Geocache
            {
                PersonID = selectedPerson.ID,
                Contents = dialog.GeocacheContents,
                Message = dialog.GeocacheMessage,
                Latitude = latestClickLocation.Latitude,
                Longitude = latestClickLocation.Longitude,
                
            };

                await database.AddAsync(geocache);
                await database.SaveChangesAsync();
           
                Location location = new Location();
                location.Latitude = geocache.Latitude;
                location.Longitude = geocache.Longitude;

                var pin = AddPin(location, geocache.Message, Colors.Black, 1, geocache);
                pin.MouseDown += Handled;
            
            }
            else
            {
                // Handle click on geocache pin here.
                MessageBox.Show("You clicked a geocache");
             
            };

        }

        private async void OnAddPersonClick(object sender, RoutedEventArgs args)
        {
            var dialog = new PersonDialog();
            dialog.Owner = this;
            dialog.ShowDialog();
            if (dialog.DialogResult == false)
            {
                return;
            }

<<<<<<< HEAD
            //string city = dialog.AddressCity;
            //string country = dialog.AddressCountry;
            //string streetName = dialog.AddressStreetName;
            //int streetNumber = dialog.AddressStreetNumber;
           
=======
          

>>>>>>> 760ea465a0aaa6bbe52a940613f54bfba37cedcb
            // Person here is added to map and the database. 

            Person person = new Person();
            person.FirstName = dialog.PersonFirstName;
            person.LastName = dialog.PersonLastName;
            person.Country = dialog.AddressCountry;
            person.City = dialog.AddressCity;
            person.StreetName = dialog.AddressStreetName;
            person.StreetNumber = dialog.AddressStreetNumber;
            person.Latitude = latestClickLocation.Latitude;
            person.Longitude = latestClickLocation.Longitude;

            await database.AddAsync(person);
            await database.SaveChangesAsync();
           
            Location location = new Location();
            location.Latitude = person.Latitude;
            location.Longitude = person.Longitude;

            var pin = AddPin(location, person.FirstName+ " "+ person.LastName + "\n" + person.StreetName + " "+ person.StreetNumber, Colors.Blue, 1, person);

            selectedPerson = person;
          

            pin.MouseDown += SelectedPerson;
        }

        private Pushpin AddPin(Location location, string tooltip, Color color, double opacity, object obj)
        {
            var Location = new Location { Latitude = location.Latitude, Longitude = location.Longitude };
            var pin = new Pushpin();
            pin.Cursor = Cursors.Hand;
            pin.Background = new SolidColorBrush(color);
            pin.Opacity = opacity;
            pin.Location = location;
            ToolTipService.SetToolTip(pin, tooltip);
            ToolTipService.SetInitialShowDelay(pin, 0);
            pin.Tag = obj;
            layer.AddChild(pin, latestClickLocation);
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

            //Lists that hold the people and their current string of found values.
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
            } UpdateMap();
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

            List<string> list = new List<string>();
            string path = dialog.FileName;
            // Write to the selected file here.

            Task readFile = Task.Run(async () =>
            {
                await Task.WhenAll();
                Person[] personpins = database.Person.Select(p => p).OrderByDescending(o => o).ToArray();
                lock (lockThis)
                {
                    foreach (Person person in personpins)
                    {
                        list.Add(person.FirstName + " | " + person.LastName +
                            " | " + person.Country + " | " + person.City +
                            " | " + person.StreetName + " | " +
                            person.StreetNumber + " | " + person.Latitude +
                            " | " + person.Longitude);

                        Geocache[] geo = database.Geocache
                            .Where(g => g.PersonID == person.ID)
                            .OrderByDescending(o => o).ToArray();

                        geo.ToList().ForEach(g => list.Add(g.ID + " | " + g.Latitude + " | " + g.Longitude + " | " + g.Contents + " | " + g.Message));

                        FoundGeocache[] founds = database.FoundGeocache
                            .Where(f => f.PersonID == person.ID)
                            .OrderByDescending(o => o).ToArray();

                        string allGeoID = "";
                        for (int i = 0; i < founds.Length; i++)
                        {
                            allGeoID += founds[i].GeoCacheID;
                            if (i < founds.Length - 1)
                            {
                                allGeoID += ", ";
                            }
                        }

                        list.Add("Found: " + allGeoID);
                        list.Add("");
                    }
                }
            });
            Task.WaitAll(readFile);
            File.WriteAllLines(path, list);
        }

        }
    }


