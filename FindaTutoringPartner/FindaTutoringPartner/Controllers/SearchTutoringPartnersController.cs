﻿using FindaTutoringPartner.Handlers.SearchTutoringPartners;
using Microsoft.AspNetCore.Mvc;

namespace FindaTutoringPartner.Controllers;

public class SearchTutoringPartnersController : Controller
{
    [HttpGet]
    public async Task<IActionResult> Start()
    {
        return RedirectToAction("School");
    }

    [HttpGet]
    public async Task<IActionResult> School()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> School(School.Command command)
    {
        if (!ModelState.IsValid)
        {
            return View();
        }

        return RedirectToAction("Subjects");
    }

    [HttpGet]
    public async Task<IActionResult> Subjects()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subjects(Subjects.Command command)
    {
        if (!ModelState.IsValid)
        {
            return View();
        }

        return RedirectToAction("TutorTypes");
    }

    [HttpGet]
    public async Task<IActionResult> TutorTypes()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TutorTypes(TutorTypes.Command command)
    {
        if (!ModelState.IsValid)
        {
            return View();
        }

        return RedirectToAction("Sessions");
    }

    [HttpGet]
    public async Task<IActionResult> Sessions()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sessions(Sessions.Command command)
    {
        if (!ModelState.IsValid)
        {
            return View();
        }

        return RedirectToAction("Results");
    }

    [HttpGet]
    public async Task<IActionResult> Results()
    {
        return View();
    }
}