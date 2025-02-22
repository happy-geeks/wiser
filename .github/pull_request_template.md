# Describe your changes

Please include a summary of the changes and relevant motivation and context.

## Type of change

Please check only ONE option.

- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)

## How was this tested?

Please describe how you tested your changes and how you confirmed this functionality is not a breaking change.

# Checklist before requesting a review
- [ ] I have reviewed and tested my changes
- [ ] I have commented my code, particularly in hard-to-understand areas
- [ ] I have made corresponding changes to the documentation
- [ ] I selected `develop` as the base branch and not `main`, or the pull request is a hotfix that needs to be done directly on `main`
- [ ] I double checked all my changes and they contain no temporary test code, no code that is commented out and no changes that are not part of this branch
- [ ] I added new unit tests for my changes if applicable

# release note update
- [ ] This change is a "nice" thing to mention to our costumers in the change log, so I added it.

<md-block>
How to add a release note: 
1. Open up <yourprojectsfolder>\wiser\FrontEnd\Modules\Dashboard\Views\Dashboard\Index.cshtml.
1. Find the id="update-log" section.
1. If there is already a "log-item" section without a version number at the top, add new `li` like the others to it and describe your changes in a costumer friendly way (so not too technical).
1. If there is no log-item at the top yet without a complete version number add it and add your `li` item there, (you can copy paste it from the comments a few lines above the update-log section).
</md-block>

# Related pull requests

Add any open pull requests from other projects that are related to this pull request that should be merged along with this one. If there are none, you can simply say "none" or "N/A", or just leave this section empty.

# Link to Asana ticket

Add a link to the Asana ticket. Note that **_ONLY_** the URL should be added, not the name of the ticket!
