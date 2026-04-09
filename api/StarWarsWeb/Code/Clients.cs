using System;

namespace StarWarsWeb.Code;


    public class Rootobject
    {
        public Personnage[] Property1 { get; set; }
    }

    public class Personnage
    {
        public int id { get; set; }
        public string name { get; set; }
        public string planet { get; set; }
        public string affiliation { get; set; }
        public int birthYear { get; set; }
    }

