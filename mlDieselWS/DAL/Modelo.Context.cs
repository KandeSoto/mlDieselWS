﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace mlDieselWS.DAL
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class CombustibleEntities : DbContext
    {
        public CombustibleEntities()
            : base("name=CombustibleEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<AutorizacionesEnergex> AutorizacionesEnergex { get; set; }
        public virtual DbSet<EmpleadoEnergex> EmpleadoEnergex { get; set; }
        public virtual DbSet<SolicitudDeposito> SolicitudDeposito { get; set; }
        public virtual DbSet<AutorizacionesParcialesEnergex> AutorizacionesParcialesEnergex { get; set; }
        public virtual DbSet<LitrosCargados> LitrosCargados { get; set; }
    }
}
