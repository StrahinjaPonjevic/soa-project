# Monitoring Setup

Ovaj monitoring dodatak pokriva:

- host CPU, RAM, disk i network throughput preko `windows_exporter`
- Docker container CPU, RAM, filesystem i network throughput preko `cAdvisor`
- prikaz metrika u `Grafana`
- scrape i skladistenje metrika u `Prometheus`

## 1. Pokretanje windows_exporter na host masini

Na Windows hostu `windows_exporter` treba da radi van Docker Compose stack-a, na portu `9182`.

Primer sa preuzetim `.exe` fajlom:

```powershell
.\windows_exporter.exe --telemetry.addr=:9182
```

Ako koristis MSI instalaciju, proveri da servis radi i da je endpoint dostupan na:

```text
http://localhost:9182/metrics
```

## 2. Pokretanje aplikacije sa monitoring dodatkom

Iz foldera `Explorer`:

```powershell
docker compose -f docker-compose.yml -f docker-compose.monitoring.yml up -d --build
```

## 3. Endpointi

- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3001`
- cAdvisor: `http://localhost:8081`

Grafana kredencijali:

- username: `admin`
- password: `admin`

## 4. Dashboardi

Grafana automatski ucitava dva dashboarda:

- `SOA Host Monitoring`
- `SOA Container Monitoring`

## Napomena za Docker Desktop na Windows-u

`cAdvisor` radi nad Linux VM-om koji koristi Docker Desktop. To je dovoljno za KT pracenje kontejnera, ali pojedine filesystem metrike mogu zavisiti od Docker Desktop ili WSL konfiguracije.
