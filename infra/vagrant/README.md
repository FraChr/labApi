# Vagrant + VirtualBox for Cattle-VM

Dette oppsettet er laget for en mer utforskende lab-løype der studentene skal behandle VM-er som "cattle" i stedet for "pets".

Det er viktig at dette er valgfritt.
Den manuelle installasjonen og server-oppsettet i den sentrale README-en fungerer fortsatt helt fint for labben,
og er ofte den enkleste og mest forutsigbare standardløypa i undervisning.

## Uke 5: self-hosted runner på VM-en

Hvis du bruker denne VM-en i uke 5 til CI/CD-opplegget, se også `docs/uke5-ci-cd-pipeline.md`.

Repoet inneholder et hjelpeskript for å installere GitHub Actions runner som service på VM-en:

```bash
cd /workspace
sudo RUNNER_URL="https://github.com/<owner>/<repo>" \
  RUNNER_TOKEN="<runner-registration-token>" \
  ./infra/vagrant/install-github-runner.sh
```

Standard labels i skriptet er `self-hosted,linux,x64,dev`, som matcher deploy-jobben i workflowen.

## Når dette oppsettet gir mening

Bruk denne løypa hvis du vil at studentene skal:

- opprette og destruere VM-er ofte
- øve på reproducerbar provisjonering
- kjøre Docker eller Podman inne i en Linux-VM
- bruke VirtualBox på tvers av Windows, macOS og Linux

## Krav på host-maskinen

- VirtualBox
- Vagrant

## Filer i denne mappen

- `Vagrantfile` – beskriver VM-en og nettverket
- `provision.sh` – installerer Docker, Podman eller begge deler

## Hvordan oppsettet fungerer

Standardoppsettet bruker:

- NAT for stabil og enkel nettverkstilgang på tvers av host-OS
- en synced folder fra repo-roten til `/workspace` inne i VM-en
- VirtualBox som provider
- Debian 12 som standard baseboks

Container-runtime styres med miljøvariabelen `CONTAINER_RUNTIME`:

- `docker`
- `podman`
- `both`

## Standard oppstart med Docker

```bash
cd infra/vagrant
vagrant up
vagrant ssh
cd /workspace
docker --version
docker compose version
```

Dette er den anbefalte startløypa hvis målet bare er å få en disponibel Linux-VM som kan kjøre containere.

## Podman i stedet for Docker

```bash
cd infra/vagrant
CONTAINER_RUNTIME=podman vagrant up
vagrant ssh
podman --version
```

Bruk dette hvis du vil at studentene skal sammenligne Docker og Podman,
eller hvis du ønsker en mer daemon-løs containerhistorie i undervisningen.

## Installer begge runtime-varianter

```bash
cd infra/vagrant
CONTAINER_RUNTIME=both vagrant up
```

Dette er nyttig hvis studentene skal eksperimentere og du ikke vil reprovisjonere VM-en bare for å bytte runtime.

## Rebuild fra scratch

```bash
cd infra/vagrant
vagrant destroy -f
vagrant up
```

Dette er selve cattle-testen.
Hvis dette fungerer uten manuelle reparasjoner, oppfører miljøet seg slik vi ønsker.

## Valgfri bridged NIC

NAT brukes som standard fordi det er mest kompatibelt på tvers av Windows, macOS og Linux.

Hvis du trenger at VM-en skal være mer direkte synlig på nettverket, kan du legge til bridged NIC:

```bash
cd infra/vagrant
BRIDGE_ADAPTER="Intel(R) Ethernet Connection" vagrant up
```

På Linux kan adapter-navn ofte være `eno1`, `eth0` eller lignende.

## Vanlig arbeidsflyt i labben

```bash
cd infra/vagrant
vagrant up
vagrant ssh
cd /workspace
docker compose -f infra/docker/docker-compose.yml up -d --build
curl http://localhost:8080/health
```

Dette bygger API-imaget fra lokal kode i repoet (fra `src/LabApi`) og er anbefalt nå.

## Bruke registry-image (for CI/CD-overgang)

Når dere vil teste flyten der image kommer fra registry (for eksempel GHCR), kan dere kjore:

```bash
cd infra/vagrant
vagrant ssh
cd /workspace
LABAPI_IMAGE=ghcr.io/geokkjer/labapi:1.0.0 docker compose -f infra/docker/docker-compose.yml up -d --no-build
curl http://localhost:8080/health
```

Dette hopper over lokal bygging av API og bruker publisert image i stedet.

## Feilsøking

Hvis noe feiler tidlig:

```bash
cd infra/vagrant
vagrant status
vagrant ssh
```

Hvis provisjoneringen må kjøres på nytt:

```bash
cd infra/vagrant
vagrant provision
```

Hvis du vil starte helt på nytt:

```bash
cd infra/vagrant
vagrant destroy -f
vagrant up
```

## Relaterte filer utenfor denne mappen

- `../docker/Dockerfile`
- `../docker/docker-compose.yml`
- `../../src/LabApi/LabApi.csproj`
