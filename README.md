# GDipSA-61: Team 5 (In5nite) – E-Waste Management System (.NET Backend)

## Overview

In5nite is an AI-enabled e-waste collection and operations management system.

This repository contains the ASP.NET Core backend, which acts as the central orchestration layer for:
- Business logic and workflows
- Secure authentication and role management
- Database persistence
- Integration with a Machine Learning (ML) prediction 
- Support for Android and web-based clients

The system enables data-driven decision making for improved planning and operations in e-waste collection.

⸻

Key Features
- ASP.NET Core backend with MVC & REST APIs
- Role-based access (Admin, Collector)
- Session-based authentication
- MySQL integration via Entity Framework Core
- ML microservice integration for bin fill prediction
- CI/CD with GitHub Actions and SonarCloud

Technology Stack
- Backend: ASP.NET Core (.NET), C#
- Database: MySQL, Entity Framework Core
- ML Integration: REST API (Python)
- CI/CD: GitHub Actions, SonarCloud
	- Secret Management 
	- Security Scanning: SAST (SonarCloud), SCA (OWASP Dependency Checker), DAST (OWASP ZAP)

Web Application: https://in5nite-e8d9b0g5cad9hrbg.southeastasia-01.azurewebsites.net/

## AI Tool Declaration

GitHub Copilot (Claude Sonnet 4.5) and ChatGPT-5.2 was used to assist with:
- Generating JUnit test cases
- Refactoring of codes based on issues flagged by SonarCloud
- Cloud Deployment, yml build files, bash scripts

We are responsible for the content and quality of the submitted work.
