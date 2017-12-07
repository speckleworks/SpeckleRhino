<template>
  <v-container fluid pa-0>
    <receiver-add></receiver-add>
    <v-layout wrap column>
      <v-flex xs12 v-for='client in clients' :key='client.restApi' flat class='receiver'>
        <receiver v-if='client.Role === 1' :client='client'></receiver>
      </v-flex>
    </v-layout>
  </v-container>
</template>

<script>
import ReceiverAdd from './ClientReceiverAdd.vue'
import Receiver from './Receiver.vue'
import { EventBus } from '../event-bus'

export default {
  name: 'AccountsManager',
  components: {
    ReceiverAdd,
    Receiver
  },
  computed: {
    clients() { 
      return this.$store.getters.clients
    }
  },
  data() {
    return {
      addClientDialog: false
    }
  },
  methods: {
    getFileClients() {
    }
  },
  mounted () {
  }
}
</script>

<style scoped>
  .receiver:last-child{
    /*margin-bottom: 100px;*/
  }
</style>