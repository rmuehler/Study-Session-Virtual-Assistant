# Virtual Assistant for Finding Study Sessions

A chat bot using Microsoft's Bot Services to help students find local study sessions offered by other students.

## Introduction

Can a virtual assistant using Microsoft Azure Bot Service, Bot Framework and SD cloud services connect students with tutors to help improve students' GPAs?

When a student needs assistance for a specified subject, the process of finding a tutor can be a long and arduous process. This process of searching for helpful tutors, eliminating tutors based on criteria and setting an official schedule with the right tutor can take an undesirable amount of time for the student. If there was an application that could make more time for the student to learn and less time searching for a tutor, that could be a beneficial tool. Our goal is to create a virtual assistant that could speed up the searching process of finding a tutor. 


## Current Scope

* Tutorial displayed before using the virtual assistant
  * Tutorial options also available through questioning the virtual assistant
    * Examples:  “Where should we meet?”, ''How do I search?”, “What time am I meeting my tutor?”
* Profile creation (name, schedule, email, optional phone number)
  * No password, single session
  * Information stored in Azure Tables
  * Profile information, including availability, can be updated via the virtual assistant
* Tutor makes available 60 minutes study session blocks where they are available
  * Student can sign up for a 1-on-1 for a study session block 
  * Students can sign up exactly 1 week in advance on a first come first served basis
* Search skill, allowing students to ask virtual assistant about availability at certain time slots
  * Integration with LUIS for language understanding
  * Students can see and list of available tutors and times, or search for a specific tutor
* LUIS converts natural language to intents handled by QnA Maker
  * QnA Maker will integrate with Azure Tables to query tutoring information
* Adaptive cards for displaying and interacting with tutoring information
* Accessible only by USF students
