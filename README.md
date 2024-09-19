# Watchful Eye

**A Real-Time Employee Monitoring Software**

## Overview

Watchful Eye is a real-time employee monitoring software designed to help managers oversee the activities on their employees' computers. The software comprises three main components: the employee side (Agent), the server side, and the manager side. The system provides four key features to monitor employees' computers, which the Agent application can execute. The data is then transmitted to the server, stored in a database, and subsequently displayed to the manager.

## School Project

**Student:** Oriyan Hamo

**Supervisor:** Eylon Hadad

**Submission Date:** 24.5.2024

## Table of Contents

1. [Introduction](#introduction)
2. [Theoretical Knowledge](#theoretical-knowledge)
3. [Database](#database)
4. [User Guide](#user-guide)
5. [Program's Flow](#program's-flow)

## Introduction

**Project Name:** Watchful Eye

**Brief Description:**  
Watchful Eye is a real-time employee monitoring software that consists of three main parts: the employee side (Agent), the server side, and the manager side. The system allows managers to monitor their employees' computer activities through four key features.

**System Objectives:**

- **Primary Objective:** Monitor employee computers in four ways and display the data visually on the manager's side.
- **Secondary Objectives:** Allow multiple computers to connect to the same manager and perform actions on each of them.

**System Boundaries:**

- An authenticated client can log in to their homepage and view all employees connected under them.
- Any request to the server that is not login or registration before authentication will be rejected.
- Managers cannot create a user with a name that already exists under another manager.

**Development Environment:**

- **Manager Side:** PyCharm Python 3.12
- **Server Side:** PyCharm Python 3.12
- **Employee Side:** Visual Studio 2022 C# Windows Forms

**Target Audience:**  
Managers of companies where employees use computers daily and require monitoring of their computer activities in real-time.

## Theoretical Knowledge

**Communication Protocol:**  
The protocol is based on TCP and JSON format. The server sends feature requests to the agent, and the agent processes the request and responds with the data.

**MD5 Hash:**  
MD5 is a hashing function used to store passwords securely in the database. Although it's not widely used today due to security vulnerabilities, it serves well for educational purposes.

**WinAPI:**  
Used for interacting with Windows OS to control and manipulate display elements and memory allocation.

**Threading:**  
The project utilizes threading to handle multiple tasks simultaneously, allowing independent client processing on the server.

## Database

**MongoDB:**  
Chosen for its document-based structure and efficient JSON storage format (BSON). It allows easy storage and retrieval of feature data from the database.

**Collections Structure:**

- **Users:** 
  - `Agents(agent_id, name, addr[ip, port], manager_username)`
  - `Managers(username, password)`
- **Data:** 
  - `FileExplorer(agent_id, data(Directories, Files, Path), timestamp)`
  - `ProcessesInfo(agent_id, data(Processes(Id, Name, FilePath, Owner, Uptime, CPUUsage, MemoryUsage)), timestamp)`
  - `ScreenCapture(agent_id, data(Image), timestamp)`
  - `Terminal(agent_id, data(Command, Output, Error), timestamp)`
    
## Program's Flow

**Manager Side:**

1. Visit the website: `<web_url>`
2. **New User:** Register using the `/register` page.
3. **Existing User:** Log in using the `/login` page.
4. **Dashboard:** View all connected agents. Download the specific executable for employees to run on their computers.
5. **Agent Control:** Select an agent to view and control specific features.

**Server Side:**

- Handles user authentication and registration via MongoDB.
- Manages connections with agents and managers using sockets and endpoints.

**Employee Side:**

- Runs the executable to connect with the server and communicate the manager's commands.
- Utilizes threads to handle new requests and send responses.
