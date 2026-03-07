# DuckGPT (wip)

An AI-powered rubber duck debugging assistant integrated directly into the Unity Editor.

https://github.com/user-attachments/assets/b5a05b76-b71f-42ee-98ab-60b29e3a4039

## Overview

DuckGPT brings the concept of rubber duck debugging directly into the Unity Editor. It allows developers to ask questions about their project while the plugin gathers relevant context and provides AI-generated feedback.

The tool can analyze selected C# scripts, console log errors, and the Unity scene hierarchy to help explain issues and suggest possible solutions based on the current state of the project.

DuckGPT also includes optional text-to-speech responses powered by ElevenLabs.

### DuckGPT is currently a **work in progress** and serves as a **proof-of-concept prototype** developed for a Bachelor’s thesis.

## Features
- AI debugging assistant integrated directly into the Unity Editor  
- Context-aware responses based on the current Unity project  
- Project context analysis including:
  - Console log errors  
  - Scene hierarchy  
  - Selected scripts  
- Conversation memory for follow-up questions  
- Integration with the **OpenAI API** for generating responses  
- Optional **text-to-speech output via ElevenLabs**  
- Custom Unity Editor interface built with **Unity UI Toolkit**

## Technologies
- Unity Editor Scripting
- C#
- Unity UI Toolkit
- OpenAI API
- ElevenLabs Text-to-Speech API


## Screenshots

### DuckGPT Editor Window
<img width="259" height="475" alt="Screenshot 2026-02-12 151549" src="https://github.com/user-attachments/assets/7d6c7160-af77-4d8f-bfc4-97f6c42993dc" />

### Configuration Window
<img width="444" height="475" alt="VisualElement duckVisual(37)" src="https://github.com/user-attachments/assets/e8bdd278-19db-4859-8358-43ba5478d0f3" />

### Custom animations for context gathering 
![Orange and Grey Y2k Retro Creative Portfolio Presentation](https://github.com/user-attachments/assets/977928ef-8969-4082-94e1-4f050e373fec)

