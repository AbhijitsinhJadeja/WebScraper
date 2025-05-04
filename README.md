# Media Scraper

A .NET Console Application to scrape media content (title, description, genre, seasons, episodes) and store it in a PostgreSQL database. It also uses Serilog for logging (console + file).

## Features

- Scrapes content using Selenium WebDriver
- Extracts media metadata including:
  - Title
  - Description
  - Genre
  - Seasons and Episodes
- Saves data into PostgreSQL with transaction safety
- Logs all activities using Serilog (debug, info, errors)

## Technologies Used

- .NET Console App
- Selenium WebDriver
- Serilog
- Npgsql (.NET PostgreSQL Driver)
- PostgreSQL
- C#

## Prerequisites

- [PostgreSQL](https://www.postgresql.org/)
- [ChromeDriver](https://sites.google.com/chromium.org/driver/) (compatible with your installed Chrome version)
- Google Chrome installed

## Getting Started

1. **Clone the Repository**
   ```bash
   git clone [https://github.com/AbhijitsinhJadeja/WebScraper.git]
   
2. **Set ConnectionString Path in Program.cs**

## Table Structure
CREATE TABLE Media (
    MediaId SERIAL PRIMARY KEY,
    Title VARCHAR(255),
    Description TEXT,
    Genre VARCHAR(100)
);

CREATE TABLE Seasons (
    SeasonId SERIAL PRIMARY KEY,
    MediaId INT,
    SeasonTitle VARCHAR(255),
    ReleaseDate DATE,
    FOREIGN KEY (MediaId) REFERENCES Media (MediaId) ON DELETE CASCADE
);

CREATE TABLE Episodes (
    EpisodeId SERIAL PRIMARY KEY,
    SeasonId INT,
    EpisodeTitle VARCHAR(255),
    Duration VARCHAR(50),
    EpisodeNumber INT,
    ReleaseDate DATE,
    Description TEXT,
    FOREIGN KEY (SeasonId) REFERENCES Seasons (SeasonId) ON DELETE CASCADE
);



