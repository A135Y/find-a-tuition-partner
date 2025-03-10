﻿Feature: Privacy Notice Page Tests
  Scenario: privacy notice page url is '/privacy'
    Given a user has arrived on the privacy page
    Then the page URL ends with '/privacy'

  Scenario: privacy notice page title is 'Privacy notice'
    Given a user has arrived on the privacy page
    Then the page's title is 'Privacy notice'

  Scenario: user clicks service name on privacy notice page
    Given a user has arrived on the privacy page
    When they click the 'Find a tuition partner' service name link
    Then they will be taken to the 'Find a tuition partner' journey start page

  Scenario: page as privacy notice header
    Given a user has arrived on the privacy page
    Then they will see the privacy notice header

  Scenario: page as link to personal information link
    Given a user has arrived on the privacy page
    Then they will see personal information link
    And the personal information link opens in a new window

  Scenario: page as link to contact form
    Given a user has arrived on the privacy page
    Then they will see contact form link
    And the contact form link opens in a new window

  Scenario: page as link to contact dfe form
    Given a user has arrived on the privacy page
    Then they will see contact form dfe link
    And the contact dfe form link opens in a new window

  Scenario: page as link to information commissioner 
    Given a user has arrived on the privacy page
    Then they will see information commissioner link
    And the contact information commissioner link opens in a new window

  Scenario: page as link to dfe contact form 
    Given a user has arrived on the privacy page
    Then they will see contact dfe contact link
    And the contact dfe link opens in a new window

  Scenario: page as link to contact secure dfe
    Given a user has arrived on the privacy page
    Then they will see contact secure dfe form link
    And the contact secure dfe link opens in a new window

  Scenario: page as link to contact secure dfe online form link
    Given a user has arrived on the privacy page
    Then they will see contact secure dfe online form link
    And the contact secure dfe online form link opens in a new window

  Scenario: page as link to home link
    Given a user has arrived on the privacy page
    When they click 'Home'
    Then they will be taken to the 'Find a tuition partner' journey start page
