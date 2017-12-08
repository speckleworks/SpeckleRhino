<template>
  <div id="app">
    <v-app dark>
      <v-tabs grow v-model='active'>
        <v-tabs-bar class='transparent' dark>
          <v-tabs-item key='clients' href='clients'>
            Clients
          </v-tabs-item>
          <v-tabs-item key='accounts' href='accounts'>
            Accounts
          </v-tabs-item>
          <v-menu open-on-hover transition="slide-x-transition" bottom right offset-y offset-left>
            <v-tabs-item slot='activator'>
                <v-icon>settings</v-icon>
            </v-tabs-item>
            <v-list>
              <v-list-tile @click='purgeClients'><v-icon>refresh</v-icon></v-list-tile>
              <v-list-tile @click='showDev'><v-icon>code</v-icon></v-list-tile>
            </v-list>
          </v-menu>
          <v-tabs-slider color='light-blue'></v-tabs-slider>
        </v-tabs-bar>
        <v-tabs-items>
          <v-tabs-content id='clients' key='clients'>
            <v-card flat>
              <client-manager></client-manager>
            </v-card>
          </v-tabs-content>        
          <v-tabs-content id='accounts' key='accounts'>
            <v-card flat>
              <accounts-manager></accounts-manager>
            </v-card>
          </v-tabs-content>        
        </v-tabs-items>
      </v-tabs>
      <v-slide-y-transition>
        <v-speed-dial v-model='fab' hover fixed bottom right direction='top' v-show='active=="clients"'>
          <v-btn slot='activator' fab v-model='fab' color='light-blue'>
            <v-icon>add</v-icon>
            <v-icon>close</v-icon>
          </v-btn>
          <v-tooltip left>
            <span>New Receiver</span>
            <v-btn fab dark color='cyan' slot='activator' @click='addReceiver'>
              <v-icon>cloud_download</v-icon>
            </v-btn>
          </v-tooltip>
          <v-tooltip left>
            <span>New Sender</span>
            <v-btn fab dark color='pink' slot='activator' @click='addSender'>
              <v-icon>cloud_upload</v-icon>
            </v-btn>
          </v-tooltip>
        </v-speed-dial>
    </v-slide-y-transition>
    <v-slide-y-transition>
        <v-speed-dial v-model='fab' hover fixed bottom right direction='top' v-show='active=="accounts"'>
          <v-btn slot='activator' fab v-model='fab' color='purple xxxlighten-2'>
            <v-icon>add</v-icon>
          </v-btn>
          <v-tooltip left>
            <span>Register New Account</span>
            <v-btn fab dark color='pink' slot='activator' @click=''>
              <v-icon>person_add</v-icon>
            </v-btn>
          </v-tooltip>
          <v-tooltip left>
            <span>Login to old account</span>
            <v-btn fab dark color='blue' slot='activator' @click=''>
              <v-icon>person</v-icon>
            </v-btn>
          </v-tooltip>
        </v-speed-dial>
    </v-slide-y-transition>
    </v-app>
  </div>
</template>

<script>
import AccountsManager from './components/AccountsManager.vue'
import ClientManager from './components/ClientManager.vue'
import { EventBus } from './event-bus'

export default {
  name: 'app',
  components: {
    AccountsManager,
    ClientManager
  },
  data () {
    return {
      fab: {},
      active: null
    }
  },
  methods: {
    showDev( ) {
      Interop.showDev()
    },
    addReceiver() {
      EventBus.$emit('show-add-receiver-dialog')
    },
    addSender() {
      EventBus.$emit('show-add-sender-dialog')
    },
    saveClients() {
      Interop.saveFileClients()
    },
    readClients() {
      Interop.getFileStreams()
    },
    purgeClients() {
      Interop.removeAllClients()
      .then( res => {})
      .catch( res => {})
    }
  }
}
</script>

<style>
body{
}
#app {
}
</style>
