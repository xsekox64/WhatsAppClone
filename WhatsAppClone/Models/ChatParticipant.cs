﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace WhatsAppClone.Models;

public partial class ChatParticipant
{
    public Guid Id { get; set; }

    public Guid ChatId { get; set; }

    public Guid UserId { get; set; }

    public virtual Chat Chat { get; set; }

    public virtual User User { get; set; }
}