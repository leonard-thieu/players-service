# toofz Players Service

[![Build status](https://ci.appveyor.com/api/projects/status/3udoy27b6tetostp/branch/master?svg=true)](https://ci.appveyor.com/project/leonard-thieu/players-service/branch/master)
[![codecov](https://codecov.io/gh/leonard-thieu/players-service/branch/master/graph/badge.svg)](https://codecov.io/gh/leonard-thieu/players-service)

## Overview

**toofz Players Service** is a backend service that handles updating [Crypt of the NecroDancer](http://necrodancer.com/) players for [toofz API](https://api.toofz.com/). 
It polls [Steam Web API](https://partner.steamgames.com/doc/webapi_overview) at regular intervals to provide up-to-date data.

---

**toofz Players Service** is a component of **toofz**. 
Information about other projects that support **toofz** can be found in the [meta-repository](https://github.com/leonard-thieu/toofz-necrodancer).

### Dependents

* [toofz API](https://github.com/leonard-thieu/api.toofz.com)

### Dependencies

* [toofz Steam](https://github.com/leonard-thieu/toofz-steam)
* [toofz Data](https://github.com/leonard-thieu/toofz-data)
* [toofz Services Core](https://github.com/leonard-thieu/toofz-services-core)
* [toofz Build](https://github.com/leonard-thieu/toofz-build)

## Requirements

* .NET Framework 4.6.1
* SQL Server

## Contributing

Contributions are welcome for toofz Players Service.

* Want to report a bug or request a feature? [File a new issue](https://github.com/leonard-thieu/players-service/issues).
* Join in design conversations.
* Fix an issue or add a new feature.
  * Aside from trivial issues, please raise a discussion before submitting a pull request.

### Development

#### Requirements

* Visual Studio 2017

#### Getting started

Open the solution file and build. Use Test Explorer to run tests.

## License

**toofz Players Service** is released under the [MIT License](LICENSE).
