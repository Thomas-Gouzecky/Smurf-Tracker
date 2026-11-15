FROM golang:1.27-alpine AS dependencies

WORKDIR /app

COPY go.mod go.sum ./

RUN go mod download

FROM dependencies AS build

COPY . ./

RUN CGO_ENABLED=0 go build -o email-service -ldflags="-s -w" .

FROM golang:1.27-alpine AS production

COPY --from=build /app/email-service /email-service

CMD ["/email-service"]