﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace WhatsAppClone.Models;

public partial class GroupMember
{
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }

    public Guid UserId { get; set; }

    public bool? IsAdmin { get; set; }

    public DateTime? JoinedAt { get; set; }

    public virtual Group Group { get; set; }

    public virtual User User { get; set; }
}