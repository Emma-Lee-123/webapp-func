using System;
using System.Collections.Generic;
using System.Xml.Serialization;

[XmlRoot("AllSchedules")]
public class AllSchedules
{
    [XmlAttribute("ErrCode")]
    public int ErrCode { get; set; }

    [XmlAttribute("ErrMsg")]
    public string ErrMsg { get; set; }

    [XmlAttribute("Day")]
    public string Day { get; set; }

    [XmlElement("ScheduledTrips")]
    public List<ScheduledTrip> ScheduledTrips { get; set; }
}

public class ScheduledTrip
{
    [XmlAttribute("VehicleType")]
    public string VehicleType { get; set; }

    [XmlAttribute("ValidDate")]
    public DateTime ValidDate { get; set; }

    [XmlAttribute("TripID")]
    public int TripID { get; set; }

    [XmlAttribute("CorridorID")]
    public int CorridorID { get; set; }

    [XmlAttribute("Dir")]
    public string Dir { get; set; }

    [XmlAttribute("CorridorCode")]
    public string CorridorCode { get; set; }

    [XmlAttribute("TripDisplay")]
    public string TripDisplay { get; set; }

    [XmlElement("Stops")]
    public List<Stop> Stops { get; set; }
}

public class Stop
{
    [XmlAttribute("Station")]
    public string Station { get; set; }

    [XmlAttribute("StationId")]
    public int StationId { get; set; }

    [XmlAttribute("ArrTime")]
    public string ArrTime { get; set; }

    [XmlAttribute("DepTime")]
    public string DepTime { get; set; }

    [XmlAttribute("IsStop")]
    public int IsStop { get; set; }
}
