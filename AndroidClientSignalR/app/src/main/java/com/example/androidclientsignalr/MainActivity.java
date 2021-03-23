package com.example.androidclientsignalr;

import android.os.Build;
import android.os.Bundle;
import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ListView;

import androidx.annotation.RequiresApi;
import androidx.appcompat.app.AppCompatActivity;

import com.microsoft.signalr.HubConnection;
import com.microsoft.signalr.HubConnectionBuilder;
import com.microsoft.signalr.HubConnectionState;

import java.time.Instant;
import java.util.ArrayList;

public class MainActivity extends AppCompatActivity {

    //UI Elements
    Button btnLogin;
    Button btnSend;
    EditText txtMessage;
    ListView chatMessages;
    ArrayList<String> messages;

    HubConnection hubConnection;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        //Get UI elements
        btnLogin = (Button) findViewById(R.id.btnLogin);
        btnSend = (Button) findViewById(R.id.btnSend);
        txtMessage = (EditText) findViewById(R.id.txtMessage);
        chatMessages = (ListView) findViewById(R.id.chatMessages);

        messages = new ArrayList<>();
        ArrayAdapter<String> adapter = new ArrayAdapter<String>(this, android.R.layout.simple_list_item_1, messages);
        chatMessages.setAdapter(adapter);


        //Create hub connection
        hubConnection = HubConnectionBuilder.create("https://settling-stinkbug.azurewebsites.net/chat").build();

        hubConnection.on("broadcastMessage", (name, message, currentTime, ugly, terms) -> {
            System.out.println("[broadcastMessage] name: " + name);
            System.out.println("[broadcastMessage] message: " + message);
            System.out.println("[broadcastMessage] currentTime: " + currentTime);
            System.out.println("[broadcastMessage] ugly: " + ugly);
            System.out.println("[broadcastMessage] terms: " + terms);


            this.runOnUiThread(new Runnable() {
                @Override
                public void run() {
                    messages.add("[" + name + "] " + message);
                    adapter.notifyDataSetChanged();
                }
            });

        }, String.class, String.class, Float.class, Boolean.class, ArrayList.class);


        hubConnection.on("echo", (name, message) -> {
            System.out.println("[echo] name: " + name);
            System.out.println("[echo] message: " + message);
        }, String.class, String.class);


        btnLogin.setOnClickListener(new View.OnClickListener() {

            @Override
            public void onClick(View v) {
                if (btnLogin.getText().toString().toLowerCase().equals("login")) {
                    if (hubConnection.getConnectionState() == HubConnectionState.DISCONNECTED) {


                        try {
                            hubConnection.start().blockingAwait();
                        } catch (Exception ex) {
                            System.out.println("Error: " + ex.getMessage());
                        }

                        if (hubConnection.getConnectionState() == HubConnectionState.CONNECTED) {
                            btnLogin.setText("logged in");

                        }
                    }
                } else if (btnLogin.getText().toString().toLowerCase().equals("logged in")) {
                    if (hubConnection.getConnectionState() == HubConnectionState.CONNECTED) {
                        hubConnection.stop();
                        btnLogin.setText("login");
                    }
                }
            }
        });

        btnSend.setOnClickListener(new View.OnClickListener() {

            @RequiresApi(api = Build.VERSION_CODES.O)
            @Override
            public void onClick(View v) {

                if (hubConnection.getConnectionState() == HubConnectionState.CONNECTED) {

                    try {
                        String text = txtMessage.getText().toString();

                        //DateTime format: "2021-03-23T15:06:22.900Z"
                        hubConnection.send("broadcastMessage", new Object[]{"android-app", text, 1, Instant.now().toString()});

                        //hubConnection.send("echo", new Object[]{"android-app", text});


                    } catch (Exception ex) {
                        System.out.println("[ERROR] " + ex.getMessage());
                    }
                }
            }
        });
    }
}