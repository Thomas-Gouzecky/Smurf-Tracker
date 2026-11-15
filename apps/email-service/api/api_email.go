package api

import (
	"net/http"

	"github.com/Thomas-Gouzecky/Smurf-Tracker/models"
	"github.com/gin-gonic/gin"
)

func (s *GinServer) SendEmail(c *gin.Context) {
	var email models.Email
	if err := c.ShouldBindJSON(&email); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	if err := s.emailService.SendEmail(c.Request.Context(), email); err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}

	c.JSON(http.StatusOK, gin.H{"message": "Email sent successfully"})
}