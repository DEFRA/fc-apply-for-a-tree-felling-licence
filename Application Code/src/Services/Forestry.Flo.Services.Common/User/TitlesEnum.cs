using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Common.User;

public enum TitlesEnum
{
    [Display(Name="Admiral")]
    Admiral,
    [Display(Name = "Admiral of the Fleet")]
    AdmiralOfTheFleet,
    [Display(Name = "Air Commodore")]
    AirCommodore,
    [Display(Name = "Air Marshal")]
    AirMarshal,
    [Display(Name = "Air Vice Marshal")]
    AirViceMarshal,
    [Display(Name = "Baron")]
    Baron,
    [Display(Name = "Brigadier")]
    Brigadier,
    [Display(Name = "Captain")]
    Captain,
    [Display(Name = "Captain (RN)")]
    CaptainRN,
    [Display(Name = "Cmdr")]
    Cmdr,
    [Display(Name = "Col")]
    Col,
    [Display(Name = "Dame")]
    Dame,
    [Display(Name = "Dr")]
    Dr,
    [Display(Name = "Duke")]
    Duke,
    [Display(Name = "Earl")]
    Earl,
    [Display(Name = "Field Marshal")]
    FieldMarshal,
    [Display(Name = "Flt. Lt")]
    FltLt,
    [Display(Name = "Flying Officer")]
    FlyingOfficer,
    [Display(Name = "General")]
    General,
    [Display(Name = "Group Captain")]
    GroupCaptain,
    [Display(Name = "HRH")]
    HRH,
    [Display(Name = "Judge")]
    Judge,
    [Display(Name = "Lady")]
    Lady,
    [Display(Name = "Lieutenant")]
    Lieutenant,
    [Display(Name = "Lieutenant (RN)")]
    LieutenantRN,
    [Display(Name = "Lieutenant General")]
    LieutenantGeneral,
    [Display(Name = "Lord")]
    Lord,
    [Display(Name = "Lt. Cmdr")]
    LtCmdr,
    [Display(Name = "Lt. Col")]
    LtCol,
    [Display(Name = "Major")]
    Major,
    [Display(Name = "Major General")]
    MajorGeneral,
    [Display(Name = "Marquess")]
    Marquess,
    [Display(Name = "Marshal of the Royal Air Force")]
    MarshalOfTheRoyalAirForce,
    [Display(Name = "Miss")]
    Miss,
    [Display(Name = "Mr")]
    Mr,
    [Display(Name = "Mrs")]
    Mrs,
    [Display(Name = "Ms")]
    Ms,
    [Display(Name = "No Title")]
    NoTitle,
    [Display(Name = "Other")]
    Other,
    [Display(Name = "Pilot Officer")]
    PilotOfficer,
    [Display(Name = "Professor")]
    Professor,
    [Display(Name = "Rear Admiral")]
    RearAdmiral,
    [Display(Name = "Rev")]
    Rev,
    [Display(Name = "Revd Canon")]
    RevdCanon,
    [Display(Name = "Reverend Father")]
    ReverendFather,
    [Display(Name = "Rt. Hon. Lord")]
    RtHonLord,
    [Display(Name = "Second-Lieutenant")]
    SecondLieutenant,
    [Display(Name = "Sir")]
    Sir,
    [Display(Name = "Squadron Leader")]
    SquadronLeader,
    [Display(Name = "Sub-Lieutenant")]
    SubLieutenant,
    [Display(Name = "The Hon")]
    TheHon,
    [Display(Name = "The Hon. Mrs")]
    TheHonMrs,
    [Display(Name = "Vice Admiral")]
    ViceAdmiral,
    [Display(Name = "Viscount")]
    Viscount,
    [Display(Name = "Wing Commander")]
    WingCommander
}