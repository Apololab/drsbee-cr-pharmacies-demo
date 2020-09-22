using System;
using System.IO;
using System.Reflection;
using Apololab.Common.Http.Exception;
using Apololab.Common.Http.Rest;
using DrsBee.API;

namespace DrsBeePharmacyApiDemo
{
    class MainClass
    {

        static DrsBeeConfig CONFIG = DrsBeeConfig.DEV_CR;
        const string API_KEY_RESOURCE = "apiClientPharmaGroupClerk1.key"; // El archivo esta incluído en el proyecto como resource, en el directorio "Resources"
        const string API_KEY_ACCOUNT = "pharmagroupclerk1@test.com";

        static UserServices userServices = new UserServices(CONFIG);
        static PharmacyServices pharmacyServices = new PharmacyServices(CONFIG);

        public static void Main(string[] args)
        {
            try
            {
                // Inicializamos el token para los servicios de API
                using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(API_KEY_RESOURCE))
                {
                    DrsBeeAuth.InitApi(API_KEY_ACCOUNT, resourceStream);
                }

                LoginSuccess login = userServices.LoginAsHealthProfessionalByCert().Result;
                if (login == null)
                {
                    throw new Exception("No se pudo realizar login");
                }

                ApiPharmacyPrescriptionsToDeliver prescriptionsToDeliver = pharmacyServices.GetPrescriptionsToDeliverAsync().Result;
                Console.WriteLine("Actualmente hay: " + prescriptionsToDeliver.prescriptionsToDeliver.Count + " prescription a entregar");
                ApiPharmacyPrescription prescriptionToDispense = prescriptionsToDeliver.prescriptionsToDeliver[prescriptionsToDeliver.prescriptionsToDeliver.Count - 1];
                Console.WriteLine("Dispensando primer prescripcion con id "+prescriptionToDispense.id+" pendiente de dispensar desde "+prescriptionToDispense.quote.lastModifiedDate);
                pharmacyServices.DispenseQuotedPrescriptionAsync( prescriptionToDispense.id ).Wait();
                Console.WriteLine("Prescription dispensada");

            }
            catch (Exception ex)
            {
                WebServiceException wsex = ex.InnerException != null ? ex.InnerException as WebServiceException : null;
                HttpException hex = ex.InnerException != null ? ex.InnerException as HttpException : null;
                // Error en drsbee backend con mensaje de usuario final incluido, Malas credenciales, malos parametros, prescripcion en estado incorrecto, etc.
                if (wsex != null)
                {
                    Console.WriteLine("El API de DrsBee retornó un error: " + wsex.Message + " de tipo " + wsex.Type);
                }
                // Error HTTP fuera drsbee, falla de conexion, timeout,etc.
                else if (hex != null)
                {
                    Console.WriteLine("Hubo un error de comunicación http con DrsBee, código: " + hex.HttpCode + "  invocando el URL: " + hex.Url);
                }
                else
                {
                    Console.WriteLine("Ocurrió un error desconocido ");
                    Console.WriteLine(ex.StackTrace);
                }
            }


        }
    }
}
