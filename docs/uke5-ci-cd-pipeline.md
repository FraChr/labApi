# Uke 5: Full CI/CD Pipeline med GitHub Actions, GHCR og self-hosted runner

Dette opplegget er laget for uke 5, etter at studentene allerede har hatt:

- CI/CD-teori
- GitHub Actions fundamentals

Målet her er å lande hele flyten i praksis:

1. Studenten pusher en endring til C#-kode
2. GitHub Actions bygger prosjektet, kjører eventuelle tester og verifiserer formattering
3. Workflow bygger et Docker-image og publiserer det til GHCR
4. En self-hosted runner på dev-VM-en plukker opp deploy-jobben
5. VM-en trekker nytt image og starter stacken med Docker Compose

Dette er bevisst enklere enn en produksjonsplattform. Poenget er å forstå sammenhengen fra commit til kjørende tjeneste.

## Hva studentene skal sitte igjen med

- De kan forklare forskjellen på CI og CD med et konkret eksempel
- De ser hvorfor GHCR eller annet registry er et naturlig mellomledd
- De forstår hvorfor en self-hosted runner kan brukes til deployment
- De kan feilsøke et enkelt pipeline-oppsett når build, auth eller deploy feiler

## Anbefalt undervisningsrekkefølge

### Del 1: Du demonstrerer hele pipeline-en

Vis den ferdige flyten én gang først:

1. Gjør en liten endring i API-et
2. Push til `main`
3. Følg workflowen i GitHub Actions
4. Vis at image dukker opp i GHCR
5. Vis at deploy-jobben kjører på self-hosted runner
6. Verifiser med `/health`

Studentene trenger først å se hele verdikjeden uten avbrudd.

### Del 2: Studentene gjenskaper den i egne forks

La studentene bygge opp samme løype i egne repos. Det gir bedre læring enn at de bare leser YAML.

### Del 3: Sikkerhet som neste steg

Når basis-pipeline-en fungerer, utvider dere den med scanning:

- dependency / SCA scanning
- container image scanning med Trivy

Det er en bedre progresjon enn å starte med scanning før studentene faktisk har en fungerende deploy-flyt.

## Arkitektur for labben

Denne labben bruker én enkel topologi:

- GitHub repo for kode og workflows
- GHCR for container-images
- én dev-VM som både er self-hosted runner og deploy-target
- Docker Compose på VM-en

Det er ikke den mest avanserte løsningen, men det er en god undervisningsløsning.

## Før du begynner

Du bør ha:

- et GitHub-repo for denne labben
- en VM med Docker og Docker Compose
- en bruker med tilgang til repoet på GitHub
- et image-navn på formen `ghcr.io/<owner>/<repo>` i lowercase

Hvis du bruker den medfølgende Vagrant-løypa, start der:

```bash
cd infra/vagrant
vagrant up
vagrant ssh
cd /workspace
docker --version
docker compose version
```

## Workflowen i repoet

Workflowen i [ci.yml](../.github/workflows/ci.yml) er satt opp slik:

- `pull_request` til `main`: bygg, kjør tester hvis repoet har testprosjekter, og verifiser formattering
- `push` til `main`: bygg, kjør tester hvis repoet har testprosjekter, bygg image, push til GHCR og deploy til dev-VM
- `workflow_dispatch`: manuell deploy av en eksisterende image-tag

Det holder modellen enkel:

- PR-er verifiserer kvalitet
- push til `main` deployer
- manuell dispatch brukes til redeploy eller rollback-demo

## GitHub-secrets og variables

Legg inn disse repository secrets før du demonstrerer deploy:

- `GHCR_USERNAME`
- `GHCR_TOKEN`
- `POSTGRES_DB`
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`

I denne versjonen feiler deploy-jobben bevisst hvis disse secrets mangler. Det er bedre enn å falle tilbake til kjente standardverdier under undervisning.

Anbefalt minimum for `GHCR_TOKEN`:

- `read:packages` for deploy på VM-en

Hvis du vil bruke private packages i GHCR, må VM-en kunne autentisere mot GHCR. Det er den enkleste grunnen til å ha `GHCR_USERNAME` og `GHCR_TOKEN` som repo-secrets.

Valgfri repository variable:

- `APP_HEALTH_URL`

Hvis den ikke settes, bruker workflowen `http://localhost:8080/health`.

## Installer self-hosted runner på dev-VM

Repoet inneholder et hjelpeskript i [install-github-runner.sh](../infra/vagrant/install-github-runner.sh).

På VM-en:

```bash
cd /workspace
sudo RUNNER_URL="https://github.com/<owner>/<repo>" \
  RUNNER_TOKEN="<runner-registration-token>" \
  ./infra/vagrant/install-github-runner.sh
```

Dette skriptet:

- installerer avhengigheter
- laster ned GitHub Actions runner
- oppretter systembrukeren `github-runner`
- registrerer runneren mot repoet
- installerer runneren som service
- legger runner-brukeren i `docker`-gruppen

Skriptet er laget for å kunne kjøres på nytt hvis du må reinstallere runneren under en lab.

Standard labels er:

- `self-hosted`
- `linux`
- `x64`
- `dev`

Det matcher deploy-jobben i workflowen.

### Hent runner-token

I GitHub:

1. Gå til repoet
2. `Settings`
3. `Actions`
4. `Runners`
5. `New self-hosted runner`

Der får du en midlertidig registration token.

## Hvordan deploymenten fungerer

Deploy-jobben kjører direkte på self-hosted runneren på VM-en. Det betyr at du ikke trenger SSH fra GitHub Actions til serveren.

Jobben gjør i praksis dette:

```bash
docker login ghcr.io
LABAPI_IMAGE=ghcr.io/<owner>/<repo>:<tag> docker compose -f infra/docker/docker-compose.yml pull api
LABAPI_IMAGE=ghcr.io/<owner>/<repo>:<tag> docker compose -f infra/docker/docker-compose.yml up -d --remove-orphans db api
curl http://localhost:8080/health
```

Dette er en god ting å vise studentene eksplisitt, fordi det fjerner litt av "GitHub Actions-magien".

Merk at `<owner>/<repo>` må være lowercase når det brukes som Docker image-navn mot GHCR.

## Verifiser at det virker

Når du har pushet en endring:

1. Åpne workflowen i GitHub Actions
2. Se at `Build, Test and Verify Formatting` lykkes
3. Se at image blir publisert til GHCR
4. Se at `Deploy To Dev VM` blir plukket opp av self-hosted runneren
5. Kjør dette på VM-en:

```bash
docker ps
docker compose -f infra/docker/docker-compose.yml ps
curl http://localhost:8080/health
```

Hvis du vil vise hvilken image-tag som faktisk kjører:

```bash
docker inspect lab-api --format '{{.Config.Image}}'
```

## Manuell redeploy / rollback-demo

Workflowen kan også kjøres manuelt med `workflow_dispatch`.

Da kan du oppgi en eksisterende image-tag, for eksempel en tidligere commit-SHA. Det er fint for å demonstrere:

- redeploy
- rollback
- at registry er mellomlaget mellom build og deploy

## Hva vi bevisst ikke gjør i denne første versjonen

For uke 5 holder vi igjen på noen ting:

- ingen Kubernetes
- ingen staging/prod-promotering
- ingen blue/green eller canary
- ingen avansert secrets management
- ingen automatisert migrasjonsstrategi utover dagens enkle schema

Det er et bevisst valg. Studentene trenger først å forstå én fungerende pipeline.

## Neste naturlige steg: sikkerhet inn i pipeline-en

Når denne pipeline-en fungerer, er det naturlig å utvide workflowen med:

- dependency scanning
- `dotnet list package --vulnerable`
- Trivy mot image i GHCR eller lokalt bygget image
- eventuelt policy: deploy bare hvis scanning er grønn

Da blir sikkerhet en del av CI/CD-flyten, ikke en egen sidehistorie.

## Forslag til undervisningsopplegg

Et enkelt opplegg for én økt kan være:

1. 15 min: tegn opp flyten repo -> Actions -> GHCR -> runner -> Compose
2. 20 min: live-demo av ferdig pipeline
3. 30 min: studentene setter opp runner og secrets i egne forks
4. 20 min: studentene pusher og feilsøker
5. 10 min: diskuter hvor Trivy og SCA bør inn

## Feilsøking

### Runner tar ikke jobben

Sjekk at runneren er online i GitHub og har label `dev`.

### Deploy feiler ved GHCR login

Sjekk `GHCR_USERNAME` og `GHCR_TOKEN`.

### Compose starter ikke API-et

Sjekk logs:

```bash
docker compose -f infra/docker/docker-compose.yml logs --tail=100 api
```

### Healthcheck feiler

Sjekk at containeren faktisk lytter på port `8080` og at `APP_HEALTH_URL` peker riktig.

## Oppsummering

Den viktigste læringen i denne labben er ikke YAML-syntaks. Det er at studentene forstår hele leveransebanen:

- kode endres
- pipeline validerer endringen
- image bygges og publiseres
- deployment henter en identifiserbar versjon
- tjenesten kan verifiseres etter deploy

Når den modellen sitter, blir både scanning, quality gates og mer avansert drift mye lettere å plassere riktig.
