Feature: User enters postcode to begin search
  Scenario: page url is '/'
    Given a user has started the 'Find a tuition partner' journey
    Then the page URL ends with '/'

  Scenario: page title is 'Find a tuition partner'
    Given a user has started the 'Find a tuition partner' journey
    Then the page's title is 'Find a tuition partner'

  Scenario: user clicks service name
    Given a user has started the 'Find a tuition partner' journey
    When they click the 'Find a tuition partner' service name link
    Then they will be taken to the 'Find a tuition partner' journey start page

  Scenario: Check other options tab displayed
    Given a user has started the 'Find a tuition partner' journey
    Then check other tutoring options text is displayed

  Scenario: Check academic mentors link
     Given a user has started the 'Find a tuition partner' journey
     Then the other options academic links to '/academic-mentors'

  Scenario: Check School led tutoring link
     Given a user has started the 'Find a tuition partner' journey
     Then the other options school-led tutoring links to '/school-led-tutoring'

  Scenario: no back link on the 'Find a tuition partner' journey start page
    Given a user has started the 'Find a tuition partner' journey
    Then the 'Back' link is not displayed

  Scenario: quality assured tuition partner details is initially hidden
    Given a user has started the 'Find a tuition partner' journey
    Then the quality assured tuition partner details are hidden

  Scenario: user clicks quality assured tuition partner details summary
    Given a user has started the 'Find a tuition partner' journey
    When they click 'How are tuition partners quality-assured?'
    Then the quality assured tuition partner details are shown
    And they will see the tribal link

  Scenario: user does not enter postcode
    Given a user has started the 'Find a tuition partner' journey
    When they click 'Continue'
    Then they will see 'Enter a postcode' as an error message for the 'postcode'

  Scenario: user enters an invalid postcode
    Given a user has started the 'Find a tuition partner' journey
    When they enter 'INVALID' as the school's postcode
    And they click 'Continue'
    Then they will see 'Enter a real postcode' as an error message for the 'postcode'

  Scenario: user enters a postcode in Wales
    Given a user has started the 'Find a tuition partner' journey
    When they enter 'SA1 1DX' as the school's postcode
    And they click 'Continue'
    Then they will see 'This service covers England only' as an error message for the 'postcode'

  Scenario: user enters a postcode in Scotland
    Given a user has started the 'Find a tuition partner' journey
    When they enter 'G33 2SQ' as the school's postcode
    And they click 'Continue'
    Then they will see 'This service covers England only' as an error message for the 'postcode'

  Scenario: user enters a postcode in Northern Ireland
    Given a user has started the 'Find a tuition partner' journey
    When they enter 'BT47 5QG' as the school's postcode
    And they click 'Continue'
    Then they will see 'This service covers England only' as an error message for the 'postcode'

  Scenario: user clicks postcode error
    Given a user has tried to continue without entering a postcode
    When they click on the postcode error
    Then the school's postcode text input is focused

  Scenario: user enters a valid postcode
    Given a user has started the 'Find a tuition partner' journey
    When they enter 'SK1 1EB' as the school's postcode
    And they click 'Continue'
    Then they will be taken to the 'Which key stages' page

  Scenario: accessiblity link to page
    Given a user has started the 'Find a tuition partner' journey
    Then the accessibility link 'Accessibility' links to '/accessibility'

  Scenario: funding link to page
    Given a user has started the 'Find a tuition partner' journey
    When they click funding and reporting link
    Then they will see the funding reporting header
    And  they will click the back link
    Then they redirects to postcode page

  Scenario: privacy link to page
    Given a user has started the 'Find a tuition partner' journey
    Then the privacy link opens privacy page
    
  Scenario: contact us link to page
    Given a user has started the 'Find a tuition partner' journey
    Then the contact us link opens contact us page

  Scenario: contact us back link redirects to page
    Given a user has started the 'Find a tuition partner' journey
    Then they will click the contact us link
    When they click 'Back'
    Then the user redirected to postcode page
