# VisionGuard - IP Camera Management Web App

VisionGuard is a comprehensive AI-ready web application for managing IP cameras, built with ASP.NET MVC on .NET 8. It provides real-time video streaming, AI-powered analysis, scheduled recording, and secure user management.

## Features

### Core Functionality
- **Camera Management**: Add, edit, delete IP cameras with automatic protocol detection (RTSP, HTTP, ONVIF)
- **Real-time Streaming**: View individual cameras or multi-camera video walls with HLS streaming
- **Scheduled Recording**: Automatic video capture based on time schedules
- **AI Integration**: OpenAI Vision API for object detection, facial recognition, and anomaly alerts
- **User Management**: Role-based access (Admin, Viewer, Guest) with ASP.NET Identity

### Advanced Features
- **Responsive UI**: Bootstrap-based interface with dark/light mode toggle
- **Multi-language Support**: Ready for English/Spanish localization
- **Security**: HTTPS, data encryption, GDPR compliance
- **Performance**: Background processing, caching, WebRTC support
- **Export**: Download recorded videos in MP4 format
- **Notifications**: Configurable alerts via email/SMS (extensible)
- **Deployment**: Docker containerization ready

## Prerequisites

- **.NET 8 SDK**
- **FFmpeg** (for video processing and streaming)
- **SQLite** (included with EF Core)
- **OpenAI API Key** (for AI features)

## Installation

1. **Clone the repository**:
   ```bash
   git clone https://github.com/leosep/visionguard.git
   cd visionguard
   ```

2. **Install dependencies**:
   ```bash
   dotnet restore
   ```

3. **Configure settings**:
   - Update `appsettings.json` with your OpenAI API key:
     ```json
     {
       "OpenAI": {
         "ApiKey": "your-openai-api-key"
       }
     }
     ```

4. **Build and run**:
   ```bash
   dotnet build
   dotnet run
   ```

5. **Access the app**:
   - Open `http://localhost:5204`
   - Login with admin credentials: `admin@anycam.com` / `Admin123!`

## Usage

### Adding Cameras
1. Navigate to Cameras > Create New
2. Enter camera details:
   - Name
   - Full Stream URL (e.g., `rtsp://ip:port/path`)
3. The app auto-detects protocol and checks online status

### Viewing Streams
- **Individual**: Cameras > View > Select camera
- **Video Wall**: Cameras > Wall (grid view)

### Scheduled Recording
1. Create a Video Clip with future Start/End times
2. Leave FilePath empty
3. The background service will record automatically

### AI Features
- Real-time frame analysis every 30 seconds
- Alerts for detected objects/anomalies
- Facial recognition (extensible)

## Architecture

### Technologies
- **Backend**: ASP.NET MVC, .NET 8, Entity Framework Core
- **Database**: SQLite
- **Frontend**: Razor Views, Bootstrap, JavaScript
- **AI**: OpenAI Vision API
- **Video**: FFmpeg for processing/streaming

### Key Components
- **Models**: Camera, VideoClip, AiEvent, LogEntry
- **Controllers**: Cameras, VideoClips, Account
- **Services**: CameraService, AiService, ScheduledRecordingService
- **Background Services**: CameraStatusService, AiProcessingService

## API Endpoints

- `GET /Cameras` - List cameras
- `POST /Cameras/Create` - Add camera
- `GET /Cameras/View/{id}` - View camera stream
- `POST /Cameras/StartRecording/{id}` - Manual recording
- `GET /VideoClips` - List recordings

## Configuration

### Environment Variables
- `OpenAI__ApiKey`: Your OpenAI API key

### Database
- Auto-migrates on startup
- Stores in `AnyCam.db`

### Video Storage
- Local: `wwwroot/videos/`
- Configurable for cloud storage

## Testing

Run unit tests:
```bash
dotnet test
```

Tests cover:
- Camera service functionality
- AI analysis
- Database operations

## Deployment

### Docker
```bash
docker build -t visionguard .
docker run -p 8080:80 visionguard
```

### Production Notes
- Use HTTPS in production
- Configure proper logging
- Set up monitoring/alerts
- Backup database regularly

## Security

- **Authentication**: ASP.NET Identity with password hashing
- **Authorization**: Role-based access control
- **Data Protection**: Encrypted sensitive data
- **GDPR**: User data handling compliant

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit changes
4. Push and create PR

## License

MIT License - see LICENSE file for details.

## Support

For issues or questions:
- Open GitHub issue
- Check logs in `logs/` directory
- Ensure FFmpeg is installed and accessible

---

**VisionGuard** - Secure, AI-powered IP camera management for modern surveillance needs.