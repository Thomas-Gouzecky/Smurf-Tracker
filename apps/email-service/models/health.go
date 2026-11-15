package models

const OKStatus = "OK"
const FailureStatus = "FAILURE"

type Health struct {
	Status string `json:"status"`
}