package api

import (
	"net/http"

	"github.com/Thomas-Gouzecky/Smurf-Tracker/email"
	"github.com/Thomas-Gouzecky/Smurf-Tracker/models"
	"github.com/gin-gonic/gin"
)

type Server interface {
	Start() error
	Readiness(c *gin.Context)
	Liveness(c *gin.Context)
}

type GinServer struct {
	ginEngine *gin.Engine
	serverPort string
	emailService *email.EmailService
}

func NewGinServer(serverPort string, emailService *email.EmailService) Server {
	server := &GinServer{
		ginEngine: gin.New(),
		serverPort: serverPort,
		emailService: emailService,
	}
	server.setupRoutes()
	return server
}

func (s *GinServer) Start() error {
	return s.ginEngine.Run()
}

func (s *GinServer) setupRoutes() {
	s.ginEngine.GET("/readiness", s.Readiness)
	s.ginEngine.GET("/liveness", s.Liveness)

	eg := s.ginEngine.Group("/email")
	eg.POST("/send", s.SendEmail)
}

func (s *GinServer) Readiness(c *gin.Context) {
	ready := true // Replace with actual readiness check logic
	if !ready {
		c.JSON(http.StatusServiceUnavailable, models.Health{Status: models.FailureStatus})
		return
	}
	c.JSON(http.StatusOK, models.Health{
		Status: models.OKStatus,
	})
}

func (s *GinServer) Liveness(c *gin.Context) {
	c.JSON(http.StatusOK, models.Health{
		Status: models.OKStatus,
	})
}
