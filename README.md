<div id="top"></div>

<div align="center">
  <img src="https://img.shields.io/github/languages/top/gspalato/foundation?style=for-the-badge"/>
  <img src="https://img.shields.io/github/issues-raw/gspalato/foundation?style=for-the-badge"/>
  <img src="https://img.shields.io/github/contributors/gspalato/foundation?style=for-the-badge">
</div>

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <br />

  <a href="https://github.com/gspalato/foundation">
    <img src="https://raw.githubusercontent.com/gspalato/foundation/master/.project/icon_circle.png" alt="Logo" width="150" height="150">
  </a>

<h3 align="center"><b>Foundation</b></h3>

  <p align="center">
    A microservice platform and back-end for my projects and deployments.
    <br />
    <a href="https://github.com/gspalato/foundation"><strong>Explore the docs »</strong></a>
    <br />
    <br />
    <a href="https://portfolio-gspalato.vercel.app">View Portfolio</a>
    ·
    <a href="https://github.com/gspalato/foundation/issues">Report Bug</a>
    ·
    <a href="https://github.com/gspalato/foundation/issues">Request Feature</a>
  </p>
</div>

<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
      <ul>
        <li><a href="#built-with">Built With</a></li>
      </ul>
    </li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#prerequisites">Prerequisites</a></li>
        <li>
          <a href="#installation">Installation</a>
          <ul>
            <li><a href="#docker-compose">Docker Compose</a></li>
          </ul>
        </li>
      </ul>
    </li>
    <li>
      <a href="#microservices">Microservices</a>
      <ul>
        <li><a href="#gateway">Gateway</a></li>
        <li><a href="#identity">Identity</a></li>
        <li><a href="#portfolio">Portfolio</a></li>
        <li><a href="#proxy">Proxy</a></li>
        <li><a href="#static">Static</a></li>
      </ul>
    </li>
    <li><a href="#roadmap">Roadmap</a></li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#contact">Contact</a></li>
  </ol>
</details>

<!-- ABOUT THE PROJECT -->

## About The Project

Foundation is a microservice back-end and platform for my projects, allowing easy deployments with an authentication server and database.

> Currently, Foundation is adding REST support and, possibly, migrating away from GraphQL; since it wasn't necessary and added more complexity.

<p align="right">(<a href="#top">back to top</a>)</p>

### Built With

- [.NET 6](https://dotnet.microsoft.com/)
- [GraphQL](https://graphql.org)
- [Docker](https://www.docker.com)

<p align="right">(<a href="#top">back to top</a>)</p>

<!-- GETTING STARTED -->

## Getting Started

### Prerequisites

- Docker
- Python 3 <span style="color:#555">(If you'll use `fctl`)</span>
- .NET 6

### Installation

#### First Steps

1. Clone the repository

```sh
git clone https://github.com/gspalato/foundation.git
```

2. Alias `fctl` and `fgen` (Optional)

```sh
alias fctl="python3 tools/fctl"
alias fgen="dotnet run ./tools/fgen"
```

3. Run `fgen` to generate boilerplate code.

```sh
fgen
```

To get a local copy up and running with:

#### Docker Compose

4. Build docker images

```sh
fctl compose build
```

5. Configure environment variables in a `.env` file:

```env
FOUNDATION_JWT_SECURITY_KEY=insert_your_256_byte_key_here

GithubToken=your_github_token_here

DatabaseHost=127.0.0.1
DatabaseUser=example
DatabasePassword=example

AwsAccessKey=...
AwsSecretAccessKey=...
AwsFoundationIdentityProfilePictureBucket=foundation.identity.avatars
AwsFoundationIdentityProfilePictureUrlFormat=https://s3.aws.[...].com/{0}/{1}
```

6. Start it

```sh
fctl compose up
```

<p align="right">(<a href="#top">back to top</a>)</p>

## Microservices

### Gateway

Is responsible for stitching all the other services' GraphQL schemas.
It's configuration requires all the other services' Docker URLs as an environment variable `SERVICE_URLS`, which should be modified on `docker-compose.yml`.

### Identity

Handles authentication and JWT tokens. Other services rely on Identity to allow access to certain data.

### Portfolio

The Portfolio microservice handles information displayed on my portfolio website.
Currently it fetches my projects from GitHub that have a `.project/metadata.yml` and updates the database every 5 minutes.

### Proxy

A NGINX instance acting as reverse proxy for the microservices and as a static file server.

### UPx

A microservice dedicated to my coursework projects' back-ends.

<p align="right">(<a href="#top">back to top</a>)</p>

<!-- ROADMAP -->

## Roadmap

- [x] Add my portfolio's front-end as a service.
- [ ] Simplify configuration.

See the [open issues](https://github.com/gspalato/foundation/issues) for a full list of proposed features (and known issues).

<p align="right">(<a href="#top">back to top</a>)</p>

<!-- CONTRIBUTING -->

## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also simply open an issue with the tag "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

<p align="right">(<a href="#top">back to top</a>)</p>

<!-- LICENSE -->

## License

Distributed under the MIT License. See `LICENSE.txt` for more information.

<p align="right">(<a href="#top">back to top</a>)</p>

<!-- CONTACT -->

## Contact

Gabriel Spalato Marques - [@gspalato](https://twitter.com/gspalato) - unreaalism@gmail.com

Project Link: [https://github.com/gspalato/foundation](https://github.com/gspalato/foundation)

<p align="right">(<a href="#top">back to top</a>)</p>

<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->

[contributors-shield]: https://img.shields.io/github/contributors/gspalato/foundation.svg?style=for-the-badge
[contributors-url]: https://github.com/gspalato/foundation/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/gspalato/foundation.svg?style=for-the-badge
[forks-url]: https://github.com/gspalato/foundation/network/members
[stars-shield]: https://img.shields.io/github/stars/gspalato/foundation.svg?style=for-the-badge
[stars-url]: https://github.com/gspalato/foundation/stargazers
[issues-shield]: https://img.shields.io/github/issues/gspalato/foundation.svg?style=for-the-badge
[issues-url]: https://github.com/gspalato/foundation/issues
[license-shield]: https://img.shields.io/github/license/gspalato/foundation.svg?style=for-the-badge
[license-url]: https://github.com/gspalato/foundation/blob/master/LICENSE.txt
[linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg?style=for-the-badge&logo=linkedin&colorB=555
[linkedin-url]: https://linkedin.com/in/gspalato
[product-screenshot]: images/screenshot.png
