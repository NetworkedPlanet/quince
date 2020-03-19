# Quince

## A simple, Git-friendly RDF repository

[![Build status](https://ci.appveyor.com/api/projects/status/30ac6s75v0jjbkry/branch/master?svg=true)](https://ci.appveyor.com/project/NetworkedPlanet/quince/branch/master)

Quince is a file-based RDF repository. It stores RDF in a simple indexed directory structure that allows
for easy matching of triple patterns by subject, predicate or object. Because Quince uses the line-oriented NQuads format for its data files,
it is also possible to use line-based diffs such as that provided by Git to tell what has changed in a
Quince store.

Quince is being used to manage the RDF data in [DataDock.io](http://datadock.io/).
