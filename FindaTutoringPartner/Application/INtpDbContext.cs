﻿using Domain;
using Microsoft.EntityFrameworkCore;

namespace Application;

public interface INtpDbContext
{
    DbSet<Address> Addresses { get; set; }
    DbSet<LocalAuthority> LocalAuthorities { get; set; }
    DbSet<Region> Regions { get; set; }
    DbSet<Subject> Subjects { get; set; }
    DbSet<TuitionPartner> TuitionPartners { get; set; }
    DbSet<TuitionPartnerLocation> TuitionPartnerLocations { get; set; }
    DbSet<TuitionType> TuitionTypes { get; set; }
    DbSet<TutorType> TutorTypes { get; set; }
    DbSet<UserSearch> UserSearches { get; set; }
    DbSet<UserSession> UserSessions { get; set; }
   
    Task<int> SaveChangesAsync();
}