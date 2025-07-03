# Archived: CreateRole Functionality

## Overview
This folder contains the archived CreateRole functionality that was removed from the SmartFleet system.

## Archived Files
- `CreateRole.cshtml` - The view for creating new roles
- `CreateRoleViewModel.cs` - The view model for role creation

## Removed Components
- CreateRole GET and POST actions from UsersController
- CreateRole button from Users/Index.cshtml
- CreateRole button from Users/ManageRoles.cshtml

## Reason for Archiving
The CreateRole functionality was archived to simplify the role management system. The system now uses predefined roles that are created during database initialization, ensuring consistency and security.

## Current Role Management
Roles are now managed through:
- Database initialization (DbInitializer.cs)
- User role assignment/removal (ManageRoles functionality)
- No dynamic role creation

## Predefined Roles
- NormalUser
- SysSupport
- FleetManager
- MaintanceManager
- commissioner
- Driver

## Date Archived
Archived on: Fri Jun 27 04:35:59 PM EEST 2025 