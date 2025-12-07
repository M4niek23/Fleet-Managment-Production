using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

public enum LicenseCategory
{
    [EnumMember(Value = "AM")]
    AM,

    [EnumMember(Value = "A1")]
    A1,

    [EnumMember(Value = "A2")]
    A2,

    [EnumMember(Value = "A")]
    A,

    [EnumMember(Value = "B1")]
    B1,

    [EnumMember(Value = "B")]
    B,

    [EnumMember(Value = "B+E")]
    B_E,

    [EnumMember(Value = "C1")]
    C1,

    [EnumMember(Value = "C1+E")]
    C1_E,

    [EnumMember(Value = "C")]
    C,

    [Display(Name = "C+E")]
    C_E,

    [EnumMember(Value = "D1")]
    D1,

    [EnumMember(Value = "D1+E")]
    D1_E,

    [EnumMember(Value = "D")]
    D,

    [EnumMember(Value = "D+E")]
    D_E,

    [EnumMember(Value = "T")]
    T
}