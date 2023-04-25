﻿using System.ComponentModel.DataAnnotations;
using CleanMicroserviceSystem.Oceanus.Contract.Abstraction;

namespace CleanMicroserviceSystem.Themis.Contract.Users;
public class UserRegisterRequest : ContractBase
{
    [Required(ErrorMessage = "User name is required")]
    public string UserName { get; set; } = default!;

    [EmailAddress(ErrorMessage = "User Email should match Email format")]
    [Required(ErrorMessage = "User Email is required")]
    public string Email { get; set; } = default!;

    [Phone(ErrorMessage = "User Phone number should match phone number format")]
    [Required(ErrorMessage = "User Phone number is required")]
    public string PhoneNumber { get; set; } = default!;

    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = default!;

    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Confirm password is required")]
    [Compare(nameof(Password), ErrorMessage = "Confirm password does not match with Password.")]
    public string ConfirmPassword { get; set; } = default!;
}
