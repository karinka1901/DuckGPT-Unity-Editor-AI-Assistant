![Unity](https://img.shields.io/badge/Unity-Editor-black?logo=unity&logoColor=white)
![Unity Editor](https://img.shields.io/badge/Unity%20Editor-Tool-000000?logo=unity&logoColor=white)
![OpenAI](https://img.shields.io/badge/OpenAI-API-412991?logo=openai&logoColor=white)
![ElevenLabs](https://img.shields.io/badge/ElevenLabs-TTS-000000)
![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)
![UI Toolkit](https://img.shields.io/badge/Unity%20UI%20Toolkit-blue)
![Blender](https://img.shields.io/badge/Blender-F5792A?logo=blender&logoColor=white)
![Bachelor Thesis](https://img.shields.io/badge/Bachelor%20Thesis-purple)
# DuckGPT (wip)

An AI-powered rubber duck debugging assistant integrated directly into the Unity Editor.

https://github.com/user-attachments/assets/b5a05b76-b71f-42ee-98ab-60b29e3a4039

## Overview
DuckGPT brings the concept of rubber duck debugging directly into the Unity Editor. It allows developers to ask questions about their project while the plugin gathers relevant context and provides AI-generated feedback.

The tool can analyze selected C# scripts, console log errors, and the Unity scene hierarchy to help explain issues and suggest possible solutions based on the current state of the project.

DuckGPT also includes optional text-to-speech responses powered by ElevenLabs.

## Status
DuckGPT is currently a **work in progress** and serves as a **proof-of-concept prototype** developed for a Bachelor’s thesis.

## Features
- AI-assisted debugging directly inside the Unity Editor using the OpenAI API
- Context-aware responses using Unity project data:
  - Scene hierarchy 
  - Console error logs
  - Selected C# scripts
- Conversation history for follow-up questions
- Optional text-to-speech responses via **ElevenLabs**
- Custom Unity Editor interface built with **UI Toolkit**

## Technologies
- Unity Editor Scripting
- C#
- Unity UI Toolkit
- Unity Style Sheets (USS)
- OpenAI API
- ElevenLabs Text-to-Speech API

## UI/Screenshots
The user interface is inspired by a retro operating system aesthetic and is built using **Unity UI Toolkit** and **Unity Style Sheets (USS)**. The duck model was created in **Blender** and rendered into a pixel-art style sprite.

### DuckGPT Editor Window
The main interface used to interact with the debugging assistant.

<img width="259" height="475" alt="Screenshot 2026-02-12 151549" src="https://github.com/user-attachments/assets/7d6c7160-af77-4d8f-bfc4-97f6c42993dc" />

### Configuration Window
Used to configure API keys, select the AI model, customize the duck chat appearance, set the Unity experience level, and define which folders are included in the full project scan.

<img width="444" height="475" alt="VisualElement duckVisual(37)" src="https://github.com/user-attachments/assets/e8bdd278-19db-4859-8358-43ba5478d0f3" />

