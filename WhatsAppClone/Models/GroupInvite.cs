﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace WhatsAppClone.Models;

public partial class GroupInvite
{
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }

    public Guid InviterId { get; set; }

    public Guid InviteeId { get; set; }

    public string Status { get; set; }

    public DateTime? SentAt { get; set; }

    public virtual Group Group { get; set; }

    public virtual User Invitee { get; set; }

    public virtual User Inviter { get; set; }
}